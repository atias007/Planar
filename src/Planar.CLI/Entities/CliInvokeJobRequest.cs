using Planar.CLI.Attributes;
using System;
using System.Collections.Generic;

namespace Planar.CLI.Entities;

public class CliInvokeJobRequest : CliJobKey
{
    [ActionProperty(LongName = "now", ShortName = "n")]
    public DateTime? NowOverrideValue { get; set; }

    [ActionProperty("d", "data")]
    public Dictionary<string, string?>? Data { get; set; }

    [ActionProperty("t", "timeout")]
    public TimeSpan? Timeout { get; set; }

    [ActionProperty("mr", "max-retries")]
    public int? MaxRetries { get; set; }

    [ActionProperty("rs", "retry-span")]
    public TimeSpan? RetrySpan { get; set; }
}