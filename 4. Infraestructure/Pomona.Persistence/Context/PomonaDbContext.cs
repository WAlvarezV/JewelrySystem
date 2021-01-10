using Microsoft.EntityFrameworkCore;
using Pomona.Domain.Entity;
using Pomona.Persistence.Context.Configuration;

namespace Pomona.Persistence.Context
{
    internal class PomonaDbContext : DbContext
    {
        public PomonaDbContext(DbContextOptions<PomonaDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ContractConfig).Assembly);
            base.OnModelCreating(modelBuilder);
        }

        #region DbSet
        public virtual DbSet<Contract> Contracts { get; set; }
        public virtual DbSet<DailyRecord> DailyRecords { get; set; }
        public virtual DbSet<IdentificationType> IdentificationTypes { get; set; }
        public virtual DbSet<Inventory> Inventories { get; set; }
        public virtual DbSet<Invoice> Invoices { get; set; }
        public virtual DbSet<ItemType> ItemTypes { get; set; }
        public virtual DbSet<Payment> Payments { get; set; }
        public virtual DbSet<Person> Persons { get; set; }
        #endregion
    }
}
