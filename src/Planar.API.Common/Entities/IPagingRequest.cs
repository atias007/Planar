namespace Planar.API.Common.Entities
{
    public interface IPagingRequest
    {
        int? PageNumber { get; set; }
        int? PageSize { get; }

        void SetPagingDefaults();
    }
}