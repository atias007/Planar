using System;

namespace Planar.API.Common.Entities
{
    public class UpdateTimeoutRequest : JobOrTriggerKey
    {
        public TimeSpan? Timeout { get; set; }
    }
}