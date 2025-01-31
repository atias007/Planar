namespace Planar.Client.Entities
{
    public class JobAuditWithInformation : JobAudit
    {
#if NETSTANDARD2_0
        public string AdditionalInfo { get; set; }
#else
        public string? AdditionalInfo { get; set; } = null!;
#endif
    }
}