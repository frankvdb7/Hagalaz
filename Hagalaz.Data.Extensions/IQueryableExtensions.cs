using System.Linq;

namespace Hagalaz.Data.Extensions
{
    public static class QueryableExtensions
    {
        public static IQueryable<T> Paging<T>(this IQueryable<T> queryable, int page, int pageSize) => queryable.Skip(pageSize * (page - 1)).Take(pageSize);
    }
}