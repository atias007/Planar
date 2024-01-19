namespace Planar.Client.Entities
{
    public class PagingRequest : IPagingRequest
    {
        public int? PageNumber { get; set; }

        public int? PageSize { get; set; }

        public void SetPagingDefaults()
        {
            PageNumber ??= 1;
            PageSize ??= 100;
        }
    }
}