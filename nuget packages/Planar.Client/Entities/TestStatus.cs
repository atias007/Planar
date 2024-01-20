namespace Planar.Client.Entities
{
    public class TestStatus
    {
        public int Status { get; set; }

        public int? EffectedRows { get; set; }

        public int ExceptionCount { get; set; }

        public int? Duration { get; set; }
    }
}