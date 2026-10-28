using JewerlySystem.Common.Infrastructure;
using JewerlySystem.Modules.Customers.Domain;
using Microsoft.EntityFrameworkCore;

namespace JewerlySystem.Modules.Customers.Infrastructure;

public class CustomersDbContext : DbContext
{
    private readonly AuditInterceptor? _auditInterceptor;

    public DbSet<Person> Persons => Set<Person>();
    public DbSet<IdentificationType> IdentificationTypes => Set<IdentificationType>();

    public CustomersDbContext(DbContextOptions<CustomersDbContext> options, AuditInterceptor? auditInterceptor = null)
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
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PersonConfig).Assembly);
    }
}
