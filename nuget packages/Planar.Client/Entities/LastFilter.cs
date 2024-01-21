namespace Planar.Client.Entities
{
    public class LastFilter : PagingRequest
    {
        public int? LastDays { get; set; }
    }
}