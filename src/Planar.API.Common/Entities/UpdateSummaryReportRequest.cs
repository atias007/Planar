namespace Planar.Api.Common.Entities
{
    public class UpdateSummaryReportRequest
    {
        public bool Enable { get; set; }
        public string? Period { get; set; }
        public string? Group { get; set; }
    }
}