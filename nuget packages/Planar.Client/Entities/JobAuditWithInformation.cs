namespace Planar.Client.Entities
{
    public class JobAuditWithInformation : JobAudit
    {
        public string? AdditionalInfo { get; set; } = null!;
    }
}