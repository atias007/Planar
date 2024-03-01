namespace Planar.Client.Entities
{
    public class JobCounters
    {
        public int TotalRuns { get; set; }

        public int SuccessRetries { get; set; }

        public int FailRetries { get; set; }

        public int Recovers { get; set; }
    }
}