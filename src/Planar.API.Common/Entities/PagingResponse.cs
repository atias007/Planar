namespace Planar.API.Common.Entities
{
    public class PagingResponse<T> where T : class
    {
        public PagingResponse()
        {
        }

        public PagingResponse(T? data, IPagingRequest request)
        {
            Data = data;
            PageNumber = request.PageNumber;
            PageSize = request.PageSize;
        }

        public T? Data { get; set; }

        public int? PageSize { get; set; }

        public int? PageNumber { get; set; }

        public int? TotalPages { get; set; }
    }
}