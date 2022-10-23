using Planar.CLI.Attributes;
using System;

namespace Planar.CLI.Entities
{
    public class CliInvokeJobRequest : CliJobOrTriggerKey
    {
        [ActionProperty(LongName = "now", ShortName = "n")]
        public DateTime NowOverrideValue { get; set; }
    }
}