using System;

namespace Planar.API.Common.Entities
{
    public class UpdateTimeoutRequest : JobOrTriggerKey
    {
        public required TimeSpan? Timeout { get; set; }
    }
}