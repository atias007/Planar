namespace Planar.Api.Common.Entities
{
    public class UpdateReportRequest
    {
        public bool? Enable { get; set; }
        public string Period { get; set; } = null!;
        public string? Group { get; set; }
    }
}