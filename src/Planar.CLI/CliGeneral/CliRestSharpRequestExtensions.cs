using Planar.API.Common.Entities;

namespace RestSharp
{
    public static class RestRequestExtensions
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
            pagingRequest.SetPagingDefaults();
            request.AddQueryPagingParameter(pagingRequest.PageSize.GetValueOrDefault(), pagingRequest.PageNumber.GetValueOrDefault());
            return request;
        }
    }
}