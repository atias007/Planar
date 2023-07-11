namespace Planar.API.Common.Entities
{
    public interface IPagingResponse
    {
        int? Count { get; }
        int PageNumber { get; }
        int PageSize { get; }
        int? TotalPages { get; }
        int? TotalRows { get; }
        bool IsLastPage { get; }
    }
}