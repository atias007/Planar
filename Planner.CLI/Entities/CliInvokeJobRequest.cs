using Planner.CLI.Attributes;
using System;

namespace Planner.CLI.Entities
{
    public class CliInvokeJobRequest : CliJobOrTriggerKey
    {
        [ActionProperty(LongName = "now", ShortName = "n")]
        public DateTime NowOverrideValue { get; set; }
    }
}