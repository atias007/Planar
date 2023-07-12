using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Planar.API.Common.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

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
            pagingRequest.SetPagingDefaults();
            var page = pagingRequest.PageNumber.GetValueOrDefault();
            var size = pagingRequest.PageSize.GetValueOrDefault();
            return source
                .Skip((page - 1) * size)
                .Take(size);
        }

        public static async Task<PagingResponse<TDestination>> ProjectToWithPagingAsyc<TSource, TDestination>
            (this IQueryable<TSource> source, IMapper mapper, IPagingRequest pagingRequest)
            where TDestination : class
        {
            var pageQuery = source.SetPaging(pagingRequest);
            var data = await mapper.ProjectTo<TDestination>(pageQuery).ToListAsync();
            var total = await source.CountAsync();
            return new PagingResponse<TDestination>(pagingRequest, data, total);
        }

        public static async Task<PagingResponse<TSource>> ToPagingListAsync<TSource>(this IQueryable<TSource> source, IPagingRequest pagingRequest)
            where TSource : class
        {
            pagingRequest.SetPagingDefaults();
            var page = pagingRequest.PageNumber.GetValueOrDefault();
            var size = pagingRequest.PageSize.GetValueOrDefault();
            var count = await source.CountAsync();
            var data = source
                .Skip((page - 1) * size)
                .Take(size)
                .ToList();

            return new PagingResponse<TSource>(pagingRequest, data, count);
        }
    }
}