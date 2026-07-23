namespace PilotageFinancier.Domain;

/// <summary>Un fichier importé, horodaté et typé. Conserve la traçabilité de la source.</summary>
public class ImportBatch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public TypeImport Type { get; set; }
    public string NomFichier { get; set; } = string.Empty;
    public DateTime ImporteLe { get; set; } = DateTime.UtcNow;
    public int NbLignes { get; set; }

    public ICollection<EcritureBrute> Lignes { get; set; } = new List<EcritureBrute>();
}
