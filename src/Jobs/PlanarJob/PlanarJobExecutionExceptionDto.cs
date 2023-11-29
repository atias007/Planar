namespace PlanarJobInner
{
    internal class PlanarJobExecutionExceptionDto
    {
        public string? Message { get; set; }
        public string? ExceptionText { get; set; }
        public string? MostInnerMessage { get; set; }
        public string? MostInnerExceptionText { get; set; }
    }
}