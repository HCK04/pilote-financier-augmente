using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PilotageFinancier.Application.Services;
using PilotageFinancier.Domain;
using PilotageFinancier.Infrastructure;
using Xunit;

namespace PilotageFinancier.Tests;

/// <summary>Tests d'intégration couche données : normalisation + agrégation + isolation tenant.</summary>
public class PipelineTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly PilotageDbContext _db;
    private readonly CurrentTenantService _tenant = new();
    private static readonly Guid T1 = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid T2 = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");

    public PipelineTests()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();
        var options = new DbContextOptionsBuilder<PilotageDbContext>().UseSqlite(_conn).Options;
        _tenant.SetTenant(T1);
        _db = new PilotageDbContext(options, _tenant);
        _db.Database.EnsureCreated();
        _db.Tenants.AddRange(
            new Tenant { Id = T1, Nom = "T1" },
            new Tenant { Id = T2, Nom = "T2" });
        _db.SaveChanges();
    }

    private void SeedBrutes(Guid tenant, Guid batch)
    {
        _db.ImportBatches.Add(new ImportBatch { Id = batch, TenantId = tenant, Type = TypeImport.EcrituresComptables, NomFichier = "test.csv", NbLignes = 3 });
        _db.EcrituresBrutes.Add(new EcritureBrute { TenantId = tenant, ImportBatchId = batch, CodeSource = "C1", Date = new(2025, 1, 10), MontantCredit = 500, MontantDebit = 200 });
        _db.EcrituresBrutes.Add(new EcritureBrute { TenantId = tenant, ImportBatchId = batch, CodeSource = "C1", Date = new(2025, 1, 10), MontantCredit = 0, MontantDebit = 100 });
        _db.EcrituresBrutes.Add(new EcritureBrute { TenantId = tenant, ImportBatchId = batch, CodeSource = "C2", Date = new(2025, 2, 5), MontantCredit = 1000, MontantDebit = 400 });
        _db.SaveChanges();
    }

    [Fact]
    public async Task Renormalisation_AppliqueLeMapping_EtConserveLesNonMappes()
    {
        SeedBrutes(T1, Guid.NewGuid());
        _db.Mappings.Add(new MappingCompte { TenantId = T1, CodeClientSource = "C1", CodePCGENormalise = "61100" });
        await _db.SaveChangesAsync();

        var n = await new NormalisationService(_db).RenormaliserAsync(T1);

        Assert.Equal(3, n);
        var codes = await _db.EcrituresNormalisees.Select(e => e.CodePCGE).ToListAsync();
        Assert.Contains("61100", codes); // C1 mappé
        Assert.Contains("C2", codes);    // C2 non mappé -> identité
    }

    [Fact]
    public async Task Agregation_Tresorerie_CalculeFluxNetParJour()
    {
        SeedBrutes(T1, Guid.NewGuid());
        await new NormalisationService(_db).RenormaliserAsync(T1);

        await new AgregationService(_db).RecalculerAsync(T1);

        var jour10 = await _db.SeriesAgregees.FirstAsync(s =>
            s.TypeSerie == TypeSerie.Tresorerie && s.Granularite == Granularite.Jour && s.Periode == new DateTime(2025, 1, 10));
        // (500-200) + (0-100) = 200
        Assert.Equal(200m, jour10.Valeur);
    }

    [Fact]
    public async Task Agregation_Budgetaire_EstCumulative()
    {
        SeedBrutes(T1, Guid.NewGuid());
        await new NormalisationService(_db).RenormaliserAsync(T1);

        await new AgregationService(_db).RecalculerAsync(T1);

        var mois = await _db.SeriesAgregees
            .Where(s => s.TypeSerie == TypeSerie.Budgetaire)
            .OrderBy(s => s.Periode).ToListAsync();
        // janvier: débits 200+100 = 300 ; février cumulé: 300 + 400 = 700
        Assert.Equal(300m, mois[0].Valeur);
        Assert.Equal(700m, mois[1].Valeur);
    }

    [Fact]
    public async Task FiltreGlobalTenant_IsoleLesDonnees()
    {
        SeedBrutes(T1, Guid.NewGuid());
        SeedBrutes(T2, Guid.NewGuid());

        _tenant.SetTenant(T1);
        var vusParT1 = await _db.EcrituresBrutes.CountAsync();
        _tenant.SetTenant(T2);
        var vusParT2 = await _db.EcrituresBrutes.CountAsync();

        Assert.Equal(3, vusParT1);
        Assert.Equal(3, vusParT2); // chacun ne voit que les siens, jamais 6
    }

    public void Dispose() { _db.Dispose(); _conn.Dispose(); }
}
