using Microsoft.EntityFrameworkCore;
using PilotageFinancier.Application.Parsing;
using PilotageFinancier.Domain;
using PilotageFinancier.Infrastructure;

namespace PilotageFinancier.Application.Services;

/// <summary>
/// Couche 1 — ingestion. Lit un fichier client et enregistre les lignes BRUTES
/// (jamais écrasées) + le budget voté versionné. N'effectue aucun mapping ici.
/// </summary>
public class IngestionService(PilotageDbContext db, ITabularParser parser)
{
    /// <summary>Importe un fichier d'écritures comptables : code;libelle;date;debit;credit.</summary>
    public async Task<ImportBatch> ImporterEcrituresAsync(
        Guid tenantId, Stream flux, string nomFichier, CancellationToken ct = default)
    {
        var lignes = parser.Lire(flux, nomFichier);
        var batch = new ImportBatch
        {
            TenantId = tenantId, Type = TypeImport.EcrituresComptables,
            NomFichier = nomFichier, NbLignes = lignes.Count
        };
        db.ImportBatches.Add(batch);

        foreach (var l in lignes)
        {
            db.EcrituresBrutes.Add(new EcritureBrute
            {
                TenantId = tenantId,
                ImportBatchId = batch.Id,
                CodeSource = Col(l, 0),
                Libelle = Col(l, 1),
                Date = TabularParser.ParseDate(Col(l, 2)),
                MontantDebit = TabularParser.ParseDecimal(Col(l, 3)),
                MontantCredit = TabularParser.ParseDecimal(Col(l, 4)),
            });
        }
        await db.SaveChangesAsync(ct);
        return batch;
    }

    /// <summary>Importe un fichier de budget voté : code;intitule;montant;exercice. Versionné par DateValidite.</summary>
    public async Task<ImportBatch> ImporterBudgetAsync(
        Guid tenantId, Stream flux, string nomFichier, CancellationToken ct = default)
    {
        var lignes = parser.Lire(flux, nomFichier);
        var batch = new ImportBatch
        {
            TenantId = tenantId, Type = TypeImport.BudgetVote,
            NomFichier = nomFichier, NbLignes = lignes.Count
        };
        db.ImportBatches.Add(batch);

        var maintenant = DateTime.UtcNow;
        foreach (var l in lignes)
        {
            db.BudgetsVotes.Add(new BudgetVote
            {
                TenantId = tenantId,
                CodePCGE = Col(l, 0),
                Exercice = int.TryParse(Col(l, 3), out var ex) ? ex : maintenant.Year,
                MontantVote = TabularParser.ParseDecimal(Col(l, 2)),
                DateValidite = maintenant,
            });
        }
        await db.SaveChangesAsync(ct);
        return batch;
    }

    private static string Col(string[] l, int i) => i < l.Length ? l[i] : string.Empty;
}
