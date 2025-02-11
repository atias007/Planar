using System.Collections.Generic;

namespace Planar.Client.Entities
{
    public interface IPagingResponse<T> where T : class
    {
        int Count { get; set; }

#if NETSTANDARD2_0
        IEnumerable<T> Data { get; set; }
#else
        IEnumerable<T>? Data { get; set; }
#endif

        bool IsLastPage { get; }
        int PageNumber { get; set; }
        int PageSize { get; set; }
        int TotalPages { get; set; }
        int TotalRows { get; set; }
    }
}