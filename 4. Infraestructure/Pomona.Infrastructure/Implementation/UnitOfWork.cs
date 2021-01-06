using Pomona.Domain.Entity;
using Pomona.Persistence.Context;
using Pomona.Persistence.Repository;

namespace Pomona.Infrastructure.Implementation
{
    internal class UnitOfWork : IUnitOfWork
    {
        private readonly PomonaDbContext _context;
        public UnitOfWork(PomonaDbContext context) => _context = context;


        public Repository<ItemType> ItemTypes => new Repository<ItemType>(_context);
        public Repository<Contract> Contracts => new Repository<Contract>(_context);

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
