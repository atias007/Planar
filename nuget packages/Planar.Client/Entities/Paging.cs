namespace Planar.Client.Entities
{
    public class Paging : IPaging
    {
        public Paging()
        {
        }

        public Paging(int? pageNumber, int? pageSize)
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