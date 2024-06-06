using System;

namespace Planar.API.Common.Entities
{
    public class UpdateIntervalRequest : JobOrTriggerKey
    {
        public required TimeSpan Interval { get; set; }
    }
}