namespace Planar.API.Common.Entities
{
    public class GetLastHistoryCallForJobRequest : PagingRequest
    {
        public int? LastDays { get; set; }
    }
}