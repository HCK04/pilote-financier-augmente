using PilotageFinancier.Infrastructure;

namespace PilotageFinancier.Api;

/// <summary>
/// Résout le tenant courant depuis l'en-tête X-Tenant-Id (sinon tenant de démonstration),
/// et l'injecte dans le service scoped consommé par le filtre global EF Core.
/// </summary>
public class TenantMiddleware(RequestDelegate next)
{
    public const string Header = "X-Tenant-Id";

    public async Task InvokeAsync(HttpContext ctx, ICurrentTenantService tenant)
    {
        var id = ctx.Request.Headers.TryGetValue(Header, out var v) && Guid.TryParse(v, out var g)
            ? g
            : DemoData.TenantId;
        tenant.SetTenant(id);
        await next(ctx);
    }
}

/// <summary>Identifiants de démonstration pour tester le module sans onboarding réel.</summary>
public static class DemoData
{
    public static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
}
