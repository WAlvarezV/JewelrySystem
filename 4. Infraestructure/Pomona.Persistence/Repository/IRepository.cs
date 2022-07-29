using Pomona.Domain.Shared;
using Pomona.Protos.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Pomona.Persistence.Repository
{
    internal interface IRepository<T> where T : class
    {
        Task<IEnumerable<T>> GetAll();
        Task<IEnumerable<T>> FindAll(Expression<Func<T, bool>> filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
            string includeProperties = "");
        T GetById(int id);

        T Insert(T entity);
        bool Update(T entity);

        Task<PaginationResponse<T>> Paginate(Pagination pagination, string includeProperties = "");
        Task<PaginationResponse<T>> FindAndPaginate(Expression<Func<T, bool>> filter, Pagination pagination, string includeProperties = "");
    }
}
