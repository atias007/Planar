using System;

namespace Planner.API.Common.Entities
{
    public class InvokeJobRequest : JobOrTriggerKey
    {
        public DateTime? NowOverrideValue { get; set; }
    }
}