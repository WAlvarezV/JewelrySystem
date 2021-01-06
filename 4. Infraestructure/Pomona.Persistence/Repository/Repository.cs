using Microsoft.EntityFrameworkCore;
using Pomona.Persistence.Context;

namespace Pomona.Persistence.Repository
{
    internal class Repository<T> : IRepository<T> where T : class
    {
        internal PomonaDbContext _context;
        internal DbSet<T> _dbSet;

        public Repository(PomonaDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public virtual T Insert(T entity)
        {
            _dbSet.Add(entity);
            return entity;
        }
    }
}
