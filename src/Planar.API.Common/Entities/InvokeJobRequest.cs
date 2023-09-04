using System;

namespace Planar.API.Common.Entities
{
    public class InvokeJobRequest : JobOrTriggerKey
    {
        public DateTime? NowOverrideValue { get; set; }
    }
}