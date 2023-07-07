namespace Planar.API.Common.Entities
{
    public abstract class PagingRequest : IPagingRequest
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