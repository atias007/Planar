namespace Planar.Client.Entities
{
    public interface IPagingRequest
    {
        int? PageNumber { get; set; }
        int? PageSize { get; }

        void SetPagingDefaults();
    }
}