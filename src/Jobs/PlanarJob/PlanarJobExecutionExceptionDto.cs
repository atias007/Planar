namespace PlanarJobInner
{
    internal class PlanarJobExecutionExceptionDto
    {
#if NETSTANDARD2_0
        public string Message { get; set; }
        public string ExceptionText { get; set; }
        public string MostInnerMessage { get; set; }
        public string MostInnerExceptionText { get; set; }
#else
        public string? Message { get; set; }
        public string? ExceptionText { get; set; }
        public string? MostInnerMessage { get; set; }
        public string? MostInnerExceptionText { get; set; }
#endif
    }
}