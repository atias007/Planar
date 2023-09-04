namespace Planar.Client.Entities
{
    public class PagingResponse<T> : IPagingResponse<T> where T : class
    {
        public List<T>? Data { get; set; }

        public int Count { get; set; }

        public int PageNumber { get; set; }

        public int PageSize { get; set; }

        public int TotalRows { get; set; }

        public int TotalPages { get; set; }

        public bool IsLastPage => PageNumber >= TotalPages;
    }
}