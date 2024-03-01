using System.Collections.Generic;

namespace Planar.Client.Entities
{
    public interface IPagingResponse<T> where T : class
    {
        int Count { get; set; }
        IEnumerable<T>? Data { get; set; }
        bool IsLastPage { get; }
        int PageNumber { get; set; }
        int PageSize { get; set; }
        int TotalPages { get; set; }
        int TotalRows { get; set; }
    }
}