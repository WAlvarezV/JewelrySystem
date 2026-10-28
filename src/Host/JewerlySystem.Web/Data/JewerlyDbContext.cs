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
    }
}
