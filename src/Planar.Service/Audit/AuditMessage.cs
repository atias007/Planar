using Microsoft.AspNetCore.Http;
using Quartz;

namespace Planar.Service.Audit;

public class AuditMessage(IHttpContextAccessor? context) : BaseAuditMessage(context)
{
    public JobKey? JobKey { get; set; }
    public TriggerKey? TriggerKey { get; set; }
    public string Description { get; set; } = null!;
    public object? AdditionalInfo { get; set; }
    public bool AddTriggerInfo { get; set; }
    public string? JobId { get; set; }
}