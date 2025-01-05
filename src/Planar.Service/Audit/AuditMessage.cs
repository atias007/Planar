using Quartz;
using System.Collections.Generic;
using System.Security.Claims;

namespace Planar.Service.Audit;

public class AuditMessage
{
    public JobKey? JobKey { get; set; }
    public TriggerKey? TriggerKey { get; set; }
    public IEnumerable<Claim>? Claims { get; set; }
    public string Description { get; set; } = null!;
    public object? AdditionalInfo { get; set; }
    public bool AddTriggerInfo { get; set; }
    public string? CliUserName { get; set; }
    public string? CliUserDomainName { get; set; }

    public string? CliIdentity
    {
        get
        {
            if (string.IsNullOrWhiteSpace(CliUserDomainName) && string.IsNullOrWhiteSpace(CliUserName)) { return null; }
            if (string.IsNullOrWhiteSpace(CliUserDomainName)) { return CliUserName; }
            return $"{CliUserDomainName}\\{CliUserName}";
        }
    }
}