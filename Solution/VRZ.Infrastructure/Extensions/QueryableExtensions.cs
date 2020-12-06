using System;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace VRZ.Infrastructure.Extensions
{
    public static class QueryableExtensions
    {
        /// <summary>
        ///     Orders the query
        ///     <param name="query" />
        ///     with all properties in
        ///     <param name="orderBy" />
        ///     separated with commas (,).
        ///     All properties may include an optional 'asc', 'ascending', 'desc' or 'descending' after
        ///     the property and separated by a single space or the default order
        ///     <param name="defaultDirection" />
        ///     is applied.
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="query"></param>
        /// <param name="orderBy"></param>
        /// <param name="defaultDirection"></param>
        /// <returns>IOrderedQueryable that can be ordered again with 'ThenBy(Descending)'</returns>
        public static IOrderedQueryable<TSource> Order<TSource>(this IQueryable<TSource> query, string orderBy,
            ListSortDirection defaultDirection = ListSortDirection.Ascending)
        {
            if (string.IsNullOrWhiteSpace(orderBy))
                return (IOrderedQueryable<TSource>)query;

            var hasOrdered = false;

            var properties = orderBy.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim());
            foreach (var propertyOrder in properties)
            {
                var spt = propertyOrder
                    .Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .ToArray();

                if (spt.Length == 0)
                    continue;

                var prop = spt[0].Trim();

                var sortDirection = defaultDirection;
                if (spt.Length > 1)
                {
                    var sort = spt[1].Trim();
                    if (sort == "asc" || sort == "ascending")
                        sortDirection = ListSortDirection.Ascending;

                    if (sort == "desc" || sort == "descending")
                        sortDirection = ListSortDirection.Descending;
                }

                var property = typeof(TSource).GetProperty(prop);
                if (property == null)
                    continue;

                var method = !hasOrdered
                    ? typeof(QueryableExtensions)
                        .GetMethod(sortDirection == ListSortDirection.Ascending
                            ? nameof(OrderByProperty)
                            : nameof(OrderByPropertyDescending))?
                        .MakeGenericMethod(typeof(TSource), property.PropertyType)
                    : typeof(QueryableExtensions)
                        .GetMethod(sortDirection == ListSortDirection.Ascending
                            ? nameof(OrderThenBy)
                            : nameof(OrderThenByDescending))?
                        .MakeGenericMethod(typeof(TSource), property.PropertyType);

                hasOrdered = true;

                query = (IOrderedQueryable<TSource>)method?.Invoke(null, new object[] { query, property });
            }

            return (IOrderedQueryable<TSource>)query;
        }


        public static IOrderedQueryable<TSource> OrderByProperty<TSource, TRet>(IQueryable<TSource> q, PropertyInfo p)
        {
            var pe = Expression.Parameter(typeof(TSource));
            Expression se = Expression.Convert(Expression.Property(pe, p), typeof(TRet));
            return q.OrderBy(Expression.Lambda<Func<TSource, TRet>>(se, pe));
        }

        public static IOrderedQueryable<TSource> OrderByPropertyDescending<TSource, TRet>(IQueryable<TSource> q,
            PropertyInfo p)
        {
            var pe = Expression.Parameter(typeof(TSource));
            Expression se = Expression.Convert(Expression.Property(pe, p), typeof(TRet));
            return q.OrderByDescending(Expression.Lambda<Func<TSource, TRet>>(se, pe));
        }

        public static IOrderedQueryable<TSource> OrderThenBy<TSource, TRet>(IOrderedQueryable<TSource> q,
            PropertyInfo p)
        {
            var pe = Expression.Parameter(typeof(TSource));
            Expression se = Expression.Convert(Expression.Property(pe, p), typeof(TRet));

            return q.ThenBy(Expression.Lambda<Func<TSource, TRet>>(se, pe));
        }

        public static IOrderedQueryable<TSource> OrderThenByDescending<TSource, TRet>(IOrderedQueryable<TSource> q,
            PropertyInfo p)
        {
            var pe = Expression.Parameter(typeof(TSource));
            Expression se = Expression.Convert(Expression.Property(pe, p), typeof(TRet));

            return q.ThenByDescending(Expression.Lambda<Func<TSource, TRet>>(se, pe));
        }
    }
}
