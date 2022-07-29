using Microsoft.EntityFrameworkCore;
using Pomona.Domain.Shared;
using Pomona.Persistence.Context;
using Pomona.Persistence.Extensions;
using Pomona.Protos.Common;
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

        public virtual bool Update(T entityToUpdate)
        {
            _dbSet.Attach(entityToUpdate);
            _context.Entry(entityToUpdate).State = EntityState.Modified;
            return true;
        }

        public async Task<PaginationResponse<T>> Paginate(Pagination paginationProto,
            string includeProperties = "")
        {
            var queryable = _dbSet.AsQueryable();
            foreach (var includeProperty in includeProperties.Split
                    (new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                queryable = queryable.Include(includeProperty);
            }
            double count = await queryable.CountAsync();
            var totalPages = (int)Math.Ceiling(count / paginationProto.Records);
            var response = await queryable.Paginate(paginationProto).ToListAsync();
            return new PaginationResponse<T>
            {
                Items = response,
                Pages = totalPages
            };
        }

        public async Task<PaginationResponse<T>> FindAndPaginate(Expression<Func<T, bool>> filter, Pagination paginationProto,
            string includeProperties = "")
        {
            var queryable = _dbSet.AsQueryable();
            foreach (var includeProperty in includeProperties.Split
                    (new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                queryable = queryable.Include(includeProperty);
            }
            queryable = queryable.Where(filter);
            double count = await queryable.CountAsync();
            var totalPages = (int)Math.Ceiling(count / paginationProto.Records);
            var response = await queryable.Paginate(paginationProto).ToListAsync();
            return new PaginationResponse<T>
            {
                Items = response,
                Pages = totalPages
            };
        }
    }
}
