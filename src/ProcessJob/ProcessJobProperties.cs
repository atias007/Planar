namespace Planar
{
    public class ProcessJobProperties
    {
        public string Path { get; set; } = string.Empty;

        public string Filename { get; set; } = string.Empty;

        public string? Arguments { get; set; }

        public string? OutputEncoding { get; set; }

        public bool WaitForExit { get; set; }

        public TimeSpan? Timeout { get; set; }
    }
}