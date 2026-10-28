using JewerlySystem.Common.Infrastructure;
using JewerlySystem.Modules.Invoicing.Domain;
using Microsoft.EntityFrameworkCore;

namespace JewerlySystem.Modules.Invoicing.Infrastructure;

public class InvoicingDbContext : DbContext
{
    private readonly AuditInterceptor? _auditInterceptor;

    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoicePayment> Payments => Set<InvoicePayment>();

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
        modelBuilder.ApplyConfiguration(new InvoiceConfig());
        modelBuilder.Entity<InvoicePayment>(b =>
        {
            b.ToTable("Payments");
            b.HasKey(p => p.Id);
            b.Property(p => p.Number).HasMaxLength(50);
            b.Property(p => p.PaymentType).HasMaxLength(50);
            b.Property(p => p.PaymentMethod).HasMaxLength(50);
        });
    }
}
