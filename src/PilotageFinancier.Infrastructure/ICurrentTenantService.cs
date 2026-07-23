namespace PilotageFinancier.Infrastructure;

/// <summary>Fournit l'identifiant du tenant courant pour le filtre global EF Core.</summary>
public interface ICurrentTenantService
{
    Guid TenantId { get; }
    void SetTenant(Guid tenantId);
}

/// <summary>Implémentation scoped : le tenant est fixé par requête (en-tête HTTP, seed, etc.).</summary>
public class CurrentTenantService : ICurrentTenantService
{
    public Guid TenantId { get; private set; }
    public void SetTenant(Guid tenantId) => TenantId = tenantId;
}
