using JewerlySystem.Common.Infrastructure;
using JewerlySystem.Modules.Contracts.Domain;
using Microsoft.EntityFrameworkCore;

namespace JewerlySystem.Modules.Contracts.Infrastructure;

public class ContractsDbContext : DbContext
{
    private readonly AuditInterceptor? _auditInterceptor;

    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<Payment> Payments => Set<Payment>();

    public ContractsDbContext(DbContextOptions<ContractsDbContext> options, AuditInterceptor? auditInterceptor = null)
        : base(options)
    {
        _auditInterceptor = auditInterceptor;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (_auditInterceptor != null)
        {
            optionsBuilder.AddInterceptors(_auditInterceptor);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ContractConfig).Assembly);
    }
}
