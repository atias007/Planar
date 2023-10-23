namespace Planar.API.Common.Entities
{
    public class ReportsStatus
    {
        public string Period { get; set; } = null!;
        public bool Enabled { get; set; }
        public string? Group { get; set; }
    }
}