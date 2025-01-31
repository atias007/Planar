namespace Planar.Client.Entities
{
    public class LastHistoryFilter : Paging
    {
#if NETSTANDARD2_0
        public string JobId { get; set; }

        public string JobGroup { get; set; }

        public string JobType { get; set; }
#else
        public string? JobId { get; set; }

        public string? JobGroup { get; set; }

        public string? JobType { get; set; }
#endif

        public long? LastDays { get; set; }
    }
}