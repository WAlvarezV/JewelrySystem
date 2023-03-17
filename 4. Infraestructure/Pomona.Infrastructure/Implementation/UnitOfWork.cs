using Pomona.Domain.Entity;
using Pomona.Models.Transient;
using Pomona.Persistence.Context;
using Pomona.Persistence.Repository;

namespace Pomona.Infrastructure.Implementation
{
    internal class UnitOfWork : IUnitOfWork
    {
        private readonly PomonaDbContext _context;
        public UnitOfWork(PomonaDbContext context) => _context = context;

        public Repository<Brand> Brands => new(_context);
        public Repository<Contract> Contracts => new(_context);
        public Repository<DailyRecord> DailyRecords => new(_context);
        public Repository<IdentificationType> IdentificationTypes => new(_context);
        public Repository<Invoice> Invoices => new(_context);
        public Repository<Item> Items => new(_context);
        public Repository<ItemType> ItemTypes => new(_context);
        public Repository<Jewel> Jewelry => new(_context);
        public Repository<Payment> Payments => new(_context);
        public Repository<Person> Persons => new(_context);
        public Repository<Watch> Watches => new(_context);

        #region SqlRaw
        public Repository<ConsolidatedRecord> ConsolidatedRecords => new(_context);
        #endregion

        public int Save() => _context.SaveChanges();

        #region Dispose
        private bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
                if (disposing)
                    _context.Dispose();
            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            System.GC.SuppressFinalize(this);
        }
        #endregion
    }
}
