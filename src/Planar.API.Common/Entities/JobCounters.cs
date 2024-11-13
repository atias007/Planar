namespace Planar.API.Common.Entities
{
    public class JobCounters
    {
        [DisplayFormat(Format = "N0")]
        public int TotalRuns { get; set; }

        [DisplayFormat(Format = "N0")]
        public int SuccessRetries { get; set; }

        [DisplayFormat(Format = "N0")]
        public int FailRetries { get; set; }

        [DisplayFormat(Format = "N0")]
        public int Recovers { get; set; }
    }
}