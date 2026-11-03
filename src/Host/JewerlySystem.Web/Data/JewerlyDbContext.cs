using JewerlySystem.Common.Domain;
using JewerlySystem.Modules.Auth.Domain;
using JewerlySystem.Modules.Auth.Infrastructure;
using JewerlySystem.Modules.Cash.Domain;
using JewerlySystem.Modules.Contracts.Domain;
using JewerlySystem.Modules.Customers.Domain;
using JewerlySystem.Modules.Inventory.Domain;
using JewerlySystem.Modules.Invoicing.Domain;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace JewerlySystem.Web.Data;

public class JewerlyDbContext : IdentityDbContext<ApplicationUser>
{
    // Auth & Passkeys
    public DbSet<PasskeyCredential> PasskeyCredentials => Set<PasskeyCredential>();

    // Contracts
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<Payment> Payments => Set<Payment>();

    // Inventory
    public DbSet<Item> Items => Set<Item>();
    public DbSet<Jewel> Jewelry => Set<Jewel>();
    public DbSet<Watch> Watches => Set<Watch>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<ItemType> ItemTypes => Set<ItemType>();

    // Cash
    public DbSet<DailyRecord> DailyRecords => Set<DailyRecord>();
    public DbSet<ConsolidatedRecord> ConsolidatedRecords => Set<ConsolidatedRecord>();

    // Customers
    public DbSet<Person> Persons => Set<Person>();
    public DbSet<IdentificationType> IdentificationTypes => Set<IdentificationType>();

    // Invoicing
    public DbSet<Invoice> Invoices => Set<Invoice>();

    // Audit & Activity Logs
    public DbSet<UserActivityLog> UserActivityLogs => Set<UserActivityLog>();

    public JewerlyDbContext(DbContextOptions<JewerlyDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply configurations from all module infrastructure assemblies
        builder.ApplyConfigurationsFromAssembly(typeof(PasskeyCredentialConfig).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(Modules.Contracts.Infrastructure.ContractConfig).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(Modules.Inventory.Infrastructure.ItemConfig).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(Modules.Cash.Infrastructure.DailyRecordConfig).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(Modules.Customers.Infrastructure.PersonConfig).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(Modules.Invoicing.Infrastructure.InvoiceConfig).Assembly);

        builder.Entity<UserActivityLog>(b =>
        {
            b.ToTable("UserActivityLogs");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.TenantId).HasMaxLength(50).IsRequired();
            b.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            b.Property(x => x.UserName).HasMaxLength(256).IsRequired();
            b.Property(x => x.ModuleKey).HasMaxLength(50).IsRequired();
            b.Property(x => x.ActionKey).HasMaxLength(100).IsRequired();
            b.Property(x => x.Category).HasMaxLength(100).IsRequired();
            b.Property(x => x.Description).HasMaxLength(500).IsRequired();
            b.Property(x => x.EntityName).HasMaxLength(100).IsRequired();
            b.Property(x => x.EntityReference).HasMaxLength(100);

            b.HasIndex(x => new { x.TenantId, x.CreatedAt });
            b.HasIndex(x => new { x.TenantId, x.UserId, x.CreatedAt });
            b.HasIndex(x => new { x.TenantId, x.ActionKey });
        });
    }
}
