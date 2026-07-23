using Microsoft.EntityFrameworkCore;
using PilotageFinancier.Domain;
using PilotageFinancier.Forecasting;
using PilotageFinancier.Infrastructure;

namespace PilotageFinancier.Application.Services;

/// <summary>
/// Couche 4 — orchestration de la prévision. Alimente le moteur SSA avec la série agrégée voulue
/// et applique, pour le budgétaire, le plafond = total du budget voté (dernière version).
/// </summary>
public class PrevisionService(PilotageDbContext db, ISsaForecaster forecaster)
{
    public async Task<ResultatPrevision> PrevoirTresorerieAsync(
        Guid tenantId, int horizonPeriods, Granularite granularite = Granularite.Jour,
        CancellationToken ct = default)
    {
        var serie = await ChargerSerieAsync(tenantId, TypeSerie.Tresorerie, granularite, ct);
        return forecaster.Prevoir(serie, horizonPeriods, TypeSerie.Tresorerie, SsaConfig.Tresorerie);
    }

    public async Task<ResultatPrevision> PrevoirBudgetaireAsync(
        Guid tenantId, int horizonPeriods, int? exercice = null, CancellationToken ct = default)
    {
        var serie = await ChargerSerieAsync(tenantId, TypeSerie.Budgetaire, Granularite.Mois, ct);
        var plafond = await CalculerPlafondAsync(tenantId, exercice, ct);
        return forecaster.Prevoir(serie, horizonPeriods, TypeSerie.Budgetaire, SsaConfig.Budgetaire, plafond);
    }

    private async Task<List<PointSerie>> ChargerSerieAsync(
        Guid tenantId, TypeSerie type, Granularite gran, CancellationToken ct) =>
        await db.SeriesAgregees
            .Where(s => s.TenantId == tenantId && s.TypeSerie == type && s.Granularite == gran)
            .OrderBy(s => s.Periode)
            .Select(s => new PointSerie(s.Periode, (float)s.Valeur))
            .ToListAsync(ct);

    /// <summary>Plafond = somme des montants votés, en ne gardant que la dernière version par code PCGE.</summary>
    private async Task<decimal> CalculerPlafondAsync(Guid tenantId, int? exercice, CancellationToken ct)
    {
        var q = db.BudgetsVotes.Where(b => b.TenantId == tenantId);
        if (exercice is not null) q = q.Where(b => b.Exercice == exercice);
        var budgets = await q.ToListAsync(ct);

        return budgets
            .GroupBy(b => new { b.CodePCGE, b.Exercice })
            .Select(g => g.OrderByDescending(x => x.DateValidite).First().MontantVote)
            .Sum();
    }
}
