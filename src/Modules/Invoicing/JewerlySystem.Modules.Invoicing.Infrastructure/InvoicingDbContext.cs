using JewerlySystem.Common.Infrastructure;
using JewerlySystem.Modules.Invoicing.Domain;
using Microsoft.EntityFrameworkCore;

namespace JewerlySystem.Modules.Invoicing.Infrastructure;

public class InvoicingDbContext : DbContext
{
    private readonly AuditInterceptor? _auditInterceptor;

    public DbSet<Invoice> Invoices => Set<Invoice>();

    public InvoicingDbContext(DbContextOptions<InvoicingDbContext> options, AuditInterceptor? auditInterceptor = null)
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
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InvoiceConfig).Assembly);
    }
}
