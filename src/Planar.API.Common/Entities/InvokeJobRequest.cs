using System;
using System.Collections.Generic;

namespace Planar.API.Common.Entities
{
    public class InvokeJobRequest : JobOrTriggerKey
    {
        public DateTime? NowOverrideValue { get; set; }

        public Dictionary<string, string?>? Data { get; set; }
    }
}