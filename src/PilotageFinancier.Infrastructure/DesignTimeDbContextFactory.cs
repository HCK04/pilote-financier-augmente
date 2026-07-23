using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PilotageFinancier.Infrastructure;

/// <summary>Fabrique utilisée uniquement par les outils EF (migrations) en design-time.</summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PilotageDbContext>
{
    public PilotageDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PilotageDbContext>()
            .UseSqlite("Data Source=pilotage.db")
            .Options;
        return new PilotageDbContext(options, new CurrentTenantService());
    }
}
