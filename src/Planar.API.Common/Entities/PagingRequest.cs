namespace Planar.API.Common.Entities
{
    public class PagingRequest : IPagingRequest
    {
        public PagingRequest()
        {
        }

        public PagingRequest(int pageNumber, int pageSize)
        {
            PageNumber = pageNumber;
            PageSize = pageSize;
        }

        public int? PageNumber { get; set; }

        public int? PageSize { get; set; }

        public void SetPagingDefaults()
        {
            PageNumber ??= 1;
            PageSize ??= 100;
        }
    }
}