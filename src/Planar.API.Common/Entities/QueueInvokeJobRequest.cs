using System;
using YamlDotNet.Serialization;

namespace Planar.API.Common.Entities
{
    public class QueueInvokeJobRequest : InvokeJobRequest
    {
        [YamlMember(Alias = "due date")]
        public DateTime DueDate { get; set; }

        [YamlMember(Alias = "timeout")]
        public TimeSpan? Timeout { get; set; }
    }
}