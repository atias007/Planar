namespace Planar.Client.Entities
{
    public class HistorySummary
    {
#if NETSTANDARD2_0
        public string JobId { get; set; }
        public string JobName { get; set; }
        public string JobGroup { get; set; }
        public string JobType { get; set; }
        public string Author { get; set; }
#else
        public string JobId { get; set; } = null!;
        public string JobName { get; set; } = null!;
        public string JobGroup { get; set; } = null!;
        public string JobType { get; set; } = null!;
        public string Author { get; set; } = null!;
#endif

        public int Total { get; set; }
        public int Success { get; set; }
        public int Fail { get; set; }
        public int Running { get; set; }
        public int Retries { get; set; }
        public int TotalEffectedRows { get; set; }
    }
}