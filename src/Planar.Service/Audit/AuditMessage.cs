namespace Planar.Service.Audit
{
    public class AuditMessage
    {
        public string JobId { get; set; } = null!;
        public string Username { get; set; } = null!;
        public string UserTitle { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string? AdditionalInfo { get; set; }
    }
}