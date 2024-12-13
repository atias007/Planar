namespace Planar.API.Common.Entities
{
    public class GetLastHistoryCallForJobRequest : PagingRequest
    {
        public string? JobId { get; set; }

        public string? JobGroup { get; set; }

        public string? JobType { get; set; }

        public int? LastDays { get; set; }
    }
}