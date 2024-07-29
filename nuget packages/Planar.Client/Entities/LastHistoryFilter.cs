namespace Planar.Client.Entities
{
    public class LastHistoryFilter : Paging
    {
        public string? JobId { get; set; }

        public string? JobGroup { get; set; }

        public string? JobType { get; set; }

        public long? LastDays { get; set; }
    }
}