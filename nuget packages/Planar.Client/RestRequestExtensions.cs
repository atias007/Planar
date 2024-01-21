using Planar.Client.Entities;
using System;

namespace RestSharp
{
    internal static class RestRequestExtensions
    {
        public static RestRequest AddQueryPagingParameter(this RestRequest request, int pageSize)
        {
            if (pageSize > 0)
            {
                request.AddQueryParameter(nameof(IPagingRequest.PageSize), pageSize);
            }

            return request;
        }

        public static RestRequest AddQueryPagingParameter(this RestRequest request, int pageSize, int pageNumber)
        {
            request.AddQueryPagingParameter(pageSize);

            if (pageNumber > 0)
            {
                request.AddQueryParameter(nameof(IPagingRequest.PageNumber), pageNumber);
            }

            return request;
        }

        public static RestRequest AddQueryPagingParameter(this RestRequest request, IPagingRequest pagingRequest)
        {
            if (pagingRequest == null) { return request; }
            pagingRequest.SetPagingDefaults();
            request.AddQueryPagingParameter(pagingRequest.PageSize.GetValueOrDefault(), pagingRequest.PageNumber.GetValueOrDefault());
            return request;
        }

        public static RestRequest AddQueryDateScope(this RestRequest request, IDateScope dateScopeRequest)
        {
            if (dateScopeRequest.FromDate.HasValue && dateScopeRequest.FromDate > DateTime.MinValue)
            {
                request.AddQueryParameter(nameof(IDateScope.FromDate), dateScopeRequest.FromDate.Value.ToString("u"));
            }

            if (dateScopeRequest.ToDate.HasValue && dateScopeRequest.ToDate > DateTime.MinValue)
            {
                request.AddQueryParameter(nameof(IDateScope.ToDate), dateScopeRequest.ToDate.Value.ToString("u"));
            }

            return request;
        }
    }
}