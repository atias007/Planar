namespace Planar.Client.Entities
{
    public class RunningJobData
    {
#if NETSTANDARD2_0
        public string Log { get; set; }
        public string Exceptions { get; set; }
#else
        public string? Log { get; set; }
        public string? Exceptions { get; set; }
#endif

        public int ExceptionsCount { get; set; }
    }
}