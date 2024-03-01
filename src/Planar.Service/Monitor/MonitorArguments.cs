namespace Planar.Service.Monitor
{
    internal struct MonitorArguments
    {
        public string? JobId { get; set; }

        public int[]? Args { get; set; }

        public bool Handle { get; set; }

        public static MonitorArguments Empty => new() { Handle = false };
    }
}