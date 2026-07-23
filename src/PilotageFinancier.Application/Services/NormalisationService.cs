using Microsoft.EntityFrameworkCore;
using PilotageFinancier.Domain;
using PilotageFinancier.Infrastructure;

namespace PilotageFinancier.Application.Services;

/// <summary>
/// Couche 2 — normalisation. Traduit les écritures BRUTES en écritures PCGE via MappingCompte.
/// Re-normalisable à volonté (sans réimport) : on purge puis reconstruit EcritureNormalisee.
/// </summary>
public class NormalisationService(PilotageDbContext db)
{
    /// <summary>
    /// (Re)construit toutes les écritures normalisées du tenant à partir des brutes + du mapping courant.
    /// Les codes sans correspondance sont conservés tels quels (mapping identité) pour ne rien perdre.
    /// </summary>
    public async Task<int> RenormaliserAsync(Guid tenantId, CancellationToken ct = default)
    {
        var mapping = await db.Mappings
            .Where(m => m.TenantId == tenantId)
            .ToDictionaryAsync(m => m.CodeClientSource, m => m.CodePCGENormalise, ct);

        // Purge des normalisées existantes (on repart de la source brute intacte).
        var anciennes = db.EcrituresNormalisees.Where(e => e.TenantId == tenantId);
        db.EcrituresNormalisees.RemoveRange(anciennes);

        var brutes = await db.EcrituresBrutes.Where(e => e.TenantId == tenantId).ToListAsync(ct);
        foreach (var b in brutes)
        {
            var code = mapping.TryGetValue(b.CodeSource, out var pcge) ? pcge : b.CodeSource;
            db.EcrituresNormalisees.Add(new EcritureNormalisee
            {
                TenantId = tenantId,
                SourceBatchId = b.ImportBatchId,
                CodePCGE = code,
                Date = b.Date,
                MontantDebit = b.MontantDebit,
                MontantCredit = b.MontantCredit,
            });
        }
        await db.SaveChangesAsync(ct);
        return brutes.Count;
    }
}
