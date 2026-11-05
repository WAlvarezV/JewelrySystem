using JewerlySystem.Common.Domain;
using JewerlySystem.Common.Infrastructure;
using JewerlySystem.Modules.Inventory.Domain;
using Microsoft.EntityFrameworkCore;

namespace JewerlySystem.Modules.Inventory.Infrastructure;

public class InventoryDbContext : DbContext
{
    private readonly AuditInterceptor? _auditInterceptor;

    public DbSet<Item> Items => Set<Item>();
    public DbSet<Jewel> Jewelry => Set<Jewel>();
    public DbSet<Watch> Watches => Set<Watch>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<ItemType> ItemTypes => Set<ItemType>();
    public DbSet<UserActivityLog> UserActivityLogs => Set<UserActivityLog>();

    public InventoryDbContext(DbContextOptions<InventoryDbContext> options, AuditInterceptor? auditInterceptor = null)
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
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ItemConfig).Assembly);

        modelBuilder.Entity<UserActivityLog>(b =>
        {
            b.ToTable("UserActivityLogs");
            b.HasKey(x => x.Id);
        });
    }
}
