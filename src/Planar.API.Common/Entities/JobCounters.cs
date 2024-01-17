namespace Planar.API.Common.Entities
{
    public class JobCounters
    {
        [DisplayFormat("N0")]
        public int TotalRuns { get; set; }

        [DisplayFormat("N0")]
        public int SuccessRetries { get; set; }

        [DisplayFormat("N0")]
        public int FailRetries { get; set; }

        [DisplayFormat("N0")]
        public int Recovers { get; set; }
    }
}