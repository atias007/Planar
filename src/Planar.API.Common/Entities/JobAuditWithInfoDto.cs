namespace Planar.API.Common.Entities
{
    public class JobAuditWithInfoDto : JobAuditDto
    {
        public string? AdditionalInfo { get; set; } = null!;
    }
}