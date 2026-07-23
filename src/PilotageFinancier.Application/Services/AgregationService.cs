using Microsoft.EntityFrameworkCore;
using PilotageFinancier.Domain;
using PilotageFinancier.Infrastructure;

namespace PilotageFinancier.Application.Services;

/// <summary>
/// Couche 3 — cache d'agrégation. Recalcule SerieAgregee à chaque import, à deux granularités :
/// trésorerie (flux nets quotidiens ET hebdomadaires) et budgétaire (dépenses cumulées mensuelles).
/// Le moteur IA ne consomme QUE cette table, jamais les écritures normalisées.
/// </summary>
public class AgregationService(PilotageDbContext db)
{
    public async Task<int> RecalculerAsync(Guid tenantId, CancellationToken ct = default)
    {
        var ecritures = await db.EcrituresNormalisees
            .Where(e => e.TenantId == tenantId)
            .OrderBy(e => e.Date)
            .ToListAsync(ct);

        var anciennes = db.SeriesAgregees.Where(s => s.TenantId == tenantId);
        db.SeriesAgregees.RemoveRange(anciennes);

        var series = new List<SerieAgregee>();
        series.AddRange(TresorerieParJour(tenantId, ecritures));
        series.AddRange(TresorerieParSemaine(tenantId, ecritures));
        series.AddRange(BudgetaireCumuleMensuel(tenantId, ecritures));

        db.SeriesAgregees.AddRange(series);
        await db.SaveChangesAsync(ct);
        return series.Count;
    }

    private static IEnumerable<SerieAgregee> TresorerieParJour(Guid t, List<EcritureNormalisee> e) =>
        e.GroupBy(x => x.Date.Date)
         .OrderBy(g => g.Key)
         .Select(g => new SerieAgregee
         {
             TenantId = t, TypeSerie = TypeSerie.Tresorerie, Granularite = Granularite.Jour,
             Periode = g.Key, Valeur = g.Sum(x => x.MontantCredit - x.MontantDebit)
         });

    private static IEnumerable<SerieAgregee> TresorerieParSemaine(Guid t, List<EcritureNormalisee> e) =>
        e.GroupBy(x => DebutSemaine(x.Date))
         .OrderBy(g => g.Key)
         .Select(g => new SerieAgregee
         {
             TenantId = t, TypeSerie = TypeSerie.Tresorerie, Granularite = Granularite.Semaine,
             Periode = g.Key, Valeur = g.Sum(x => x.MontantCredit - x.MontantDebit)
         });

    /// <summary>Exécution budgétaire = dépenses (débits) cumulées mois après mois.</summary>
    private static IEnumerable<SerieAgregee> BudgetaireCumuleMensuel(Guid t, List<EcritureNormalisee> e)
    {
        var mensuel = e.GroupBy(x => new DateTime(x.Date.Year, x.Date.Month, 1))
                       .OrderBy(g => g.Key)
                       .Select(g => (Periode: g.Key, Depense: g.Sum(x => x.MontantDebit)));
        decimal cumul = 0m;
        foreach (var m in mensuel)
        {
            cumul += m.Depense;
            yield return new SerieAgregee
            {
                TenantId = t, TypeSerie = TypeSerie.Budgetaire, Granularite = Granularite.Mois,
                Periode = m.Periode, Valeur = cumul
            };
        }
    }

    private static DateTime DebutSemaine(DateTime d)
    {
        int diff = (7 + (int)d.DayOfWeek - (int)DayOfWeek.Monday) % 7;
        return d.Date.AddDays(-diff);
    }
}
