using Microsoft.EntityFrameworkCore;
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
    }
}