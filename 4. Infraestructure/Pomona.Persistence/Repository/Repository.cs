using Microsoft.EntityFrameworkCore;
using Pomona.Persistence.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

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

        public virtual async Task<IEnumerable<T>> GetAll() => await _dbSet.ToArrayAsync().ConfigureAwait(false);

        public virtual async Task<IEnumerable<T>> FindAll(
            Expression<Func<T, bool>> filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
            string includeProperties = "")
        {
            return await Task.Run(() =>
            {
                IQueryable<T> query = _dbSet;
                if (filter != null)
                {
                    query = query.Where(filter);
                }

                foreach (var includeProperty in includeProperties.Split
                    (new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProperty);
                }

                if (orderBy != null)
                {
                    return orderBy(query);
                }
                else
                {
                    return query;
                }
            }).ConfigureAwait(false);
        }

        public virtual T GetById(int id) => _dbSet.Find(id);

        public virtual T Insert(T entity)
        {
            _dbSet.Add(entity);
            return entity;
        }
    }
}
