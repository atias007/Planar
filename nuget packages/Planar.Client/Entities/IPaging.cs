namespace Planar.Client.Entities
{
    public interface IPaging
    {
        int? PageNumber { get; set; }
        int? PageSize { get; }

        void SetPagingDefaults();
    }
}