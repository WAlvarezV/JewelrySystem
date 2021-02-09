using Pomona.Domain.Entity;
using Pomona.Persistence.Context;
using Pomona.Persistence.Repository;

namespace Pomona.Infrastructure.Implementation
{
    internal class UnitOfWork : IUnitOfWork
    {
        private readonly PomonaDbContext _context;
        public UnitOfWork(PomonaDbContext context) => _context = context;

        public Repository<Brand> Brands => new Repository<Brand>(_context);
        public Repository<Contract> Contracts => new Repository<Contract>(_context);
        public Repository<DailyRecord> DailyRecords => new Repository<DailyRecord>(_context);
        public Repository<IdentificationType> IdentificationTypes => new Repository<IdentificationType>(_context);
        public Repository<Invoice> Invoices => new Repository<Invoice>(_context);
        public Repository<Item> Items => new Repository<Item>(_context);
        public Repository<ItemType> ItemTypes => new Repository<ItemType>(_context);
        public Repository<Jewel> Jewelry => new Repository<Jewel>(_context);
        public Repository<Payment> Payments => new Repository<Payment>(_context);
        public Repository<Person> Persons => new Repository<Person>(_context);
        public Repository<Watch> Watches => new Repository<Watch>(_context);

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
