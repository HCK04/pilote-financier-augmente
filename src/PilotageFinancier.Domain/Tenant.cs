namespace PilotageFinancier.Domain;

/// <summary>Client isolé du module (établissement public). Racine du cloisonnement multi-tenant.</summary>
public class Tenant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nom { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public DateTime CreeLe { get; set; } = DateTime.UtcNow;

    public ICollection<ImportBatch> Imports { get; set; } = new List<ImportBatch>();
    public ICollection<MappingCompte> Mappings { get; set; } = new List<MappingCompte>();
}
