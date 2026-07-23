using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace PilotageFinancier.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Enregistre le DbContext SQLite et le service de tenant courant.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddScoped<ICurrentTenantService, CurrentTenantService>();
        services.AddDbContext<PilotageDbContext>(o => o.UseSqlite(connectionString));
        return services;
    }
}
