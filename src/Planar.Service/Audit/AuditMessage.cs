using Quartz;
using System.Collections.Generic;
using System.Security.Claims;

namespace Planar.Service.Audit
{
    public class AuditMessage
    {
        public JobKey? JobKey { get; set; }
        public TriggerKey? TriggerKey { get; set; }
        public IEnumerable<Claim>? Claims { get; set; }
        public string Description { get; set; } = null!;
        public object? AdditionalInfo { get; set; }
        public bool AddTriggerInfo { get; set; }
    }
}