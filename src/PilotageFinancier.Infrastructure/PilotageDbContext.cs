using Microsoft.EntityFrameworkCore;
using PilotageFinancier.Domain;

namespace PilotageFinancier.Infrastructure;

/// <summary>
/// Contexte EF Core du module. Applique un filtre global par TenantId sur toutes les
/// entités tenant-scoped afin d'éviter toute fuite de données entre clients.
/// </summary>
public class PilotageDbContext : DbContext
{
    private readonly ICurrentTenantService _tenant;

    public PilotageDbContext(DbContextOptions<PilotageDbContext> options, ICurrentTenantService tenant)
        : base(options)
    {
        _tenant = tenant;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();
    public DbSet<EcritureBrute> EcrituresBrutes => Set<EcritureBrute>();
    public DbSet<MappingCompte> Mappings => Set<MappingCompte>();
    public DbSet<EcritureNormalisee> EcrituresNormalisees => Set<EcritureNormalisee>();
    public DbSet<BudgetVote> BudgetsVotes => Set<BudgetVote>();
    public DbSet<SerieAgregee> SeriesAgregees => Set<SerieAgregee>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Tenant>().HasKey(t => t.Id);

        b.Entity<ImportBatch>().HasIndex(x => new { x.TenantId, x.Type });
        b.Entity<ImportBatch>().HasQueryFilter(x => x.TenantId == _tenant.TenantId);

        b.Entity<EcritureBrute>().HasIndex(x => new { x.TenantId, x.ImportBatchId });
        b.Entity<EcritureBrute>().Property(x => x.MontantDebit).HasPrecision(18, 2);
        b.Entity<EcritureBrute>().Property(x => x.MontantCredit).HasPrecision(18, 2);
        b.Entity<EcritureBrute>().HasQueryFilter(x => x.TenantId == _tenant.TenantId);

        b.Entity<MappingCompte>().HasIndex(x => new { x.TenantId, x.CodeClientSource }).IsUnique();
        b.Entity<MappingCompte>().HasQueryFilter(x => x.TenantId == _tenant.TenantId);

        b.Entity<EcritureNormalisee>().Ignore(x => x.FluxNet);
        b.Entity<EcritureNormalisee>().HasIndex(x => new { x.TenantId, x.Date });
        b.Entity<EcritureNormalisee>().Property(x => x.MontantDebit).HasPrecision(18, 2);
        b.Entity<EcritureNormalisee>().Property(x => x.MontantCredit).HasPrecision(18, 2);
        b.Entity<EcritureNormalisee>().HasQueryFilter(x => x.TenantId == _tenant.TenantId);

        b.Entity<BudgetVote>().HasIndex(x => new { x.TenantId, x.CodePCGE, x.Exercice, x.DateValidite });
        b.Entity<BudgetVote>().Property(x => x.MontantVote).HasPrecision(18, 2);
        b.Entity<BudgetVote>().HasQueryFilter(x => x.TenantId == _tenant.TenantId);

        b.Entity<SerieAgregee>().HasIndex(x => new { x.TenantId, x.TypeSerie, x.Granularite, x.Periode });
        b.Entity<SerieAgregee>().Property(x => x.Valeur).HasPrecision(18, 2);
        b.Entity<SerieAgregee>().HasQueryFilter(x => x.TenantId == _tenant.TenantId);

        base.OnModelCreating(b);
    }
}
