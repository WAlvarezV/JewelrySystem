
using Pomona.Protos.Common;
using System.Linq;

namespace Pomona.Persistence.Extensions
{
    internal static class QueryableExtensions
    {
        public static IQueryable<T> Paginate<T>(this IQueryable<T> queryable, Pagination pagination)
        {
            return queryable
                .Skip((pagination.Page - 1) * pagination.Records)
                .Take(pagination.Records);
        }

        public static IQueryable<T> GetQuantity<T>(this IQueryable<T> queryable, int quantity)
        => queryable.Take(quantity);
    }
}
