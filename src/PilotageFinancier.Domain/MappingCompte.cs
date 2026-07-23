namespace PilotageFinancier.Domain;

/// <summary>Correspondance code source client -> code PCGE normalisé, par tenant.</summary>
public class MappingCompte
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string CodeClientSource { get; set; } = string.Empty;
    public string CodePCGENormalise { get; set; } = string.Empty;
    public DateTime CreeLe { get; set; } = DateTime.UtcNow;
}
