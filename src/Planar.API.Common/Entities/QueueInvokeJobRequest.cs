using System;

namespace Planar.API.Common.Entities
{
    public class QueueInvokeJobRequest : JobOrTriggerKey
    {
        public DateTime DueDate { get; set; }
        public TimeSpan? Timeout { get; set; }
    }
}