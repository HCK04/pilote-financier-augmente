namespace PilotageFinancier.Domain;

/// <summary>
/// Ligne brute telle qu'importée du fichier client, AVANT mapping PCGE.
/// Jamais écrasée : permet la re-normalisation sans réimport si le mapping est corrigé.
/// </summary>
public class EcritureBrute
{
    public long Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ImportBatchId { get; set; }

    public string CodeSource { get; set; } = string.Empty;
    public string? Libelle { get; set; }
    public DateTime Date { get; set; }
    public decimal MontantDebit { get; set; }
    public decimal MontantCredit { get; set; }

    public ImportBatch? ImportBatch { get; set; }
}
