using IdentityServer4.EntityFramework.Options;
using Microsoft.AspNetCore.ApiAuthorization.IdentityServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Pomona.Domain.Entity;
using Pomona.Persistence.Context.Configuration;
using Pomona.Pwa.Server.Models;

namespace Pomona.Pwa.Server.Data
{
    internal class ApplicationDbContext : ApiAuthorizationDbContext<ApplicationUser>
    {
        public ApplicationDbContext(
            DbContextOptions options,
            IOptions<OperationalStoreOptions> operationalStoreOptions) : base(options, operationalStoreOptions)
        {
        }

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
