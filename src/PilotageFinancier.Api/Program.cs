using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PilotageFinancier.Api;
using PilotageFinancier.Application;
using PilotageFinancier.Application.Services;
using PilotageFinancier.Domain;
using PilotageFinancier.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var cs = builder.Configuration.GetConnectionString("Pilotage") ?? "Data Source=pilotage.db";
builder.Services.AddInfrastructure(cs);
builder.Services.AddApplication();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o => o.SwaggerDoc("v1",
    new() { Title = "Pilote Financier Augmenté", Version = "v1" }));

var app = builder.Build();

// Migration + seed du tenant de démonstration.
using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    sp.GetRequiredService<ICurrentTenantService>().SetTenant(DemoData.TenantId);
    var db = sp.GetRequiredService<PilotageDbContext>();
    db.Database.Migrate();
    if (!await db.Tenants.AnyAsync(t => t.Id == DemoData.TenantId))
    {
        db.Tenants.Add(new Tenant { Id = DemoData.TenantId, Nom = "Établissement Démo", Reference = "DEMO" });
        await db.SaveChangesAsync();
    }
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseSwagger();
app.UseSwaggerUI();
app.UseMiddleware<TenantMiddleware>();

// --- Couche 1 : ingestion ---
var api = app.MapGroup("/api");

api.MapPost("/import/ecritures", async (IFormFile fichier, IngestionService ing, ICurrentTenantService t) =>
{
    await using var s = fichier.OpenReadStream();
    var batch = await ing.ImporterEcrituresAsync(t.TenantId, s, fichier.FileName);
    return Results.Ok(new { batch.Id, batch.NbLignes, batch.Type });
}).DisableAntiforgery().WithSummary("Importer un fichier d'écritures comptables (CSV/Excel)");

api.MapPost("/import/budget", async (IFormFile fichier, IngestionService ing, ICurrentTenantService t) =>
{
    await using var s = fichier.OpenReadStream();
    var batch = await ing.ImporterBudgetAsync(t.TenantId, s, fichier.FileName);
    return Results.Ok(new { batch.Id, batch.NbLignes, batch.Type });
}).DisableAntiforgery().WithSummary("Importer le fichier de budget voté (CSV/Excel)");

// --- Couche 1/2 : mapping + (re)normalisation + agrégation ---
api.MapPost("/mapping", async (MappingDto[] maps, PilotageDbContext db, ICurrentTenantService t) =>
{
    foreach (var m in maps)
    {
        var existant = await db.Mappings.FirstOrDefaultAsync(x => x.CodeClientSource == m.CodeSource);
        if (existant is null)
            db.Mappings.Add(new MappingCompte { TenantId = t.TenantId, CodeClientSource = m.CodeSource, CodePCGENormalise = m.CodePCGE });
        else
            existant.CodePCGENormalise = m.CodePCGE;
    }
    await db.SaveChangesAsync();
    return Results.Ok(new { enregistres = maps.Length });
}).WithSummary("Définir/mettre à jour le mapping compte source -> PCGE");

api.MapPost("/recalculer", async (NormalisationService norm, AgregationService agg, ICurrentTenantService t) =>
{
    var n = await norm.RenormaliserAsync(t.TenantId);
    var s = await agg.RecalculerAsync(t.TenantId);
    return Results.Ok(new { ecrituresNormalisees = n, pointsSerie = s });
}).WithSummary("Re-normaliser (PCGE) puis recalculer le cache d'agrégation");

// --- Couche 4/5 : prévisions ---
api.MapGet("/previsions/tresorerie", async (int horizon, Granularite? granularite,
    PrevisionService prev, IHubContext<PrevisionsHub> hub, ICurrentTenantService t) =>
{
    var r = await prev.PrevoirTresorerieAsync(t.TenantId, horizon, granularite ?? Granularite.Jour);
    await hub.Clients.All.SendAsync("PrevisionTresorerie", r);
    return Results.Ok(r);
}).WithSummary("Prévision de trésorerie (horizon configurable)");

api.MapGet("/previsions/budgetaire", async (int horizon, int? exercice,
    PrevisionService prev, IHubContext<PrevisionsHub> hub, ICurrentTenantService t) =>
{
    var r = await prev.PrevoirBudgetaireAsync(t.TenantId, horizon, exercice);
    await hub.Clients.All.SendAsync("PrevisionBudgetaire", r);
    return Results.Ok(r);
}).WithSummary("Prévision d'exécution budgétaire (alerte de dépassement de plafond)");

// --- État & démonstration (confort UI) ---
api.MapGet("/etat", async (PilotageDbContext db, ICurrentTenantService t) =>
{
    var ecr = db.EcrituresNormalisees.Where(e => e.TenantId == t.TenantId);
    var nbEcritures = await ecr.CountAsync();
    var min = nbEcritures > 0 ? await ecr.MinAsync(e => (DateTime?)e.Date) : null;
    var max = nbEcritures > 0 ? await ecr.MaxAsync(e => (DateTime?)e.Date) : null;
    var budgets = await db.BudgetsVotes.Where(b => b.TenantId == t.TenantId).ToListAsync();
    var plafond = budgets
        .GroupBy(b => new { b.CodePCGE, b.Exercice })
        .Select(g => g.OrderByDescending(x => x.DateValidite).First().MontantVote)
        .Sum();
    var exercice = budgets.Count > 0 ? budgets.Max(b => b.Exercice) : (int?)null;
    var nbPoints = await db.SeriesAgregees.Where(s => s.TenantId == t.TenantId).CountAsync();
    return Results.Ok(new { nbEcritures, nbPoints, plafond, exercice, dateMin = min, dateMax = max });
}).WithSummary("État courant des données du tenant");

api.MapPost("/demo/seed", async (PilotageDbContext db, NormalisationService norm,
    AgregationService agg, ICurrentTenantService t) =>
{
    var res = await DemoDataGenerator.SeedAsync(db, t.TenantId);
    await norm.RenormaliserAsync(t.TenantId);
    await agg.RecalculerAsync(t.TenantId);
    return Results.Ok(res);
}).WithSummary("Charger un jeu de données de démonstration (un clic)");

app.MapHub<PrevisionsHub>("/hubs/previsions");

app.Run();

/// <summary>DTO d'une ligne de mapping compte source -> PCGE.</summary>
public record MappingDto(string CodeSource, string CodePCGE);

public partial class Program { }
