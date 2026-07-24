using PilotageFinancier.Domain;
using PilotageFinancier.Infrastructure;

namespace PilotageFinancier.Api;

/// <summary>
/// Génère un jeu de données de démonstration réaliste (écritures + budget voté) pour un tenant,
/// afin d'offrir une première expérience « en un clic » sans onboarding réel.
/// </summary>
public static class DemoDataGenerator
{
    private static readonly (string Code, string Libelle)[] Postes =
    {
        ("61100", "Achats de matières"),
        ("61110", "Achats de fournitures"),
        ("62000", "Services extérieurs"),
        ("70000", "Produits / recettes"),
    };

    public static async Task<object> SeedAsync(PilotageDbContext db, Guid tenantId, int jours = 150)
    {
        // Purge des données existantes du tenant (idempotent).
        db.SeriesAgregees.RemoveRange(db.SeriesAgregees.Where(x => x.TenantId == tenantId));
        db.EcrituresNormalisees.RemoveRange(db.EcrituresNormalisees.Where(x => x.TenantId == tenantId));
        db.EcrituresBrutes.RemoveRange(db.EcrituresBrutes.Where(x => x.TenantId == tenantId));
        db.BudgetsVotes.RemoveRange(db.BudgetsVotes.Where(x => x.TenantId == tenantId));
        db.ImportBatches.RemoveRange(db.ImportBatches.Where(x => x.TenantId == tenantId));
        db.Mappings.RemoveRange(db.Mappings.Where(x => x.TenantId == tenantId));
        await db.SaveChangesAsync();

        var rng = new Random(7);
        var debut = new DateTime(DateTime.UtcNow.Year, 1, 1);

        var batch = new ImportBatch { TenantId = tenantId, Type = TypeImport.EcrituresComptables, NomFichier = "demo.csv" };
        db.ImportBatches.Add(batch);

        var nb = 0;
        for (var d = 0; d < jours; d++)
        {
            var date = debut.AddDays(d);
            // Tendance de dépense + saisonnalité hebdomadaire + bruit.
            var baseDep = 900 + 350 * Math.Sin(d / 7.0) + d * 4;
            var depense = (decimal)Math.Max(200, baseDep + rng.Next(-250, 250));
            var poste = Postes[d % 3]; // postes de charge
            db.EcrituresBrutes.Add(new EcritureBrute
            {
                TenantId = tenantId, ImportBatchId = batch.Id, CodeSource = poste.Code,
                Libelle = poste.Libelle, Date = date, MontantDebit = depense, MontantCredit = 0
            });
            nb++;

            if (d % 2 == 0) // recettes un jour sur deux
            {
                var recette = (decimal)(1600 + 900 * Math.Cos(d / 9.0) + rng.Next(-300, 600));
                db.EcrituresBrutes.Add(new EcritureBrute
                {
                    TenantId = tenantId, ImportBatchId = batch.Id, CodeSource = "70000",
                    Libelle = "Recette", Date = date, MontantDebit = 0, MontantCredit = Math.Max(0, recette)
                });
                nb++;
            }
        }
        batch.NbLignes = nb;

        // Budget voté (plafonds) + mapping identité.
        var exercice = debut.Year;
        db.BudgetsVotes.AddRange(
            new BudgetVote { TenantId = tenantId, CodePCGE = "61100", Exercice = exercice, MontantVote = 120000 },
            new BudgetVote { TenantId = tenantId, CodePCGE = "61110", Exercice = exercice, MontantVote = 60000 },
            new BudgetVote { TenantId = tenantId, CodePCGE = "62000", Exercice = exercice, MontantVote = 40000 });
        foreach (var p in Postes)
            db.Mappings.Add(new MappingCompte { TenantId = tenantId, CodeClientSource = p.Code, CodePCGENormalise = p.Code });

        await db.SaveChangesAsync();
        return new { ecritures = nb, exercice, plafond = 220000m };
    }
}
