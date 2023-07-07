using Microsoft.EntityFrameworkCore;
using Planar.API.Common.Entities;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Planar.Service.Data
{
    public static class DataExtensions
    {
        public static void RemoveRange<TEntity>(this DbSet<TEntity> entities, Expression<Func<TEntity, bool>> predicate)
        where TEntity : class
        {
            var records = entities.Where(predicate).ToList();
            if (records.Count > 0)
            {
                entities.RemoveRange(records);
            }
        }

        public static IQueryable<TSource> SetPaging<TSource>(this IQueryable<TSource> source, IPagingRequest pagingRequest)
        {
            var pageNumber = pagingRequest.PageNumber > 0 ? pagingRequest.PageNumber.Value : 1;
            var pageSize = pagingRequest.PageSize > 0 ? pagingRequest.PageSize.Value : 100;

            return source
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize);
        }
    }
}