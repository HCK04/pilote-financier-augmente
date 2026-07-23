namespace PilotageFinancier.Domain;

/// <summary>
/// Écriture traduite dans la nomenclature PCGE générique.
/// Produite par re-normalisation depuis EcritureBrute + MappingCompte.
/// Seule source consommée pour construire les séries agrégées.
/// </summary>
public class EcritureNormalisee
{
    public long Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid SourceBatchId { get; set; }

    public string CodePCGE { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal MontantDebit { get; set; }
    public decimal MontantCredit { get; set; }

    /// <summary>Flux net (crédit - débit), utile pour la série trésorerie.</summary>
    public decimal FluxNet => MontantCredit - MontantDebit;
}
