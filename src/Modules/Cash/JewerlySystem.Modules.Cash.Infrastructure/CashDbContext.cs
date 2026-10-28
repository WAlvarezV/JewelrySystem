using JewerlySystem.Common.Infrastructure;
using JewerlySystem.Modules.Cash.Domain;
using Microsoft.EntityFrameworkCore;

namespace JewerlySystem.Modules.Cash.Infrastructure;

public class CashDbContext : DbContext
{
    private readonly AuditInterceptor? _auditInterceptor;

    public DbSet<DailyRecord> DailyRecords => Set<DailyRecord>();
    public DbSet<ConsolidatedRecord> ConsolidatedRecords => Set<ConsolidatedRecord>();

    public CashDbContext(DbContextOptions<CashDbContext> options, AuditInterceptor? auditInterceptor = null)
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
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DailyRecordConfig).Assembly);
    }
}
