namespace Planar.API.Common.Entities
{
    public class RunningJobData
    {
        public string? Log { get; set; }

        public string? Exceptions { get; set; }

        public int ExceptionsCount { get; set; }
    }
}