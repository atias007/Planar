using System;
using System.Collections.Generic;

namespace PlanarJob;

internal class InvokeJobModel
{
    public string Id { get; set; } = null!;
    public InvokeJobOptions Options { get; set; } = null!;
}

internal class QueueInvokeJobModel : InvokeJobModel
{
    public DateTime DueDate { get; set; }
}

internal class InvokeJobOptions
{
    public DateTime? NowOverrideValue { get; set; }

    public TimeSpan? Timeout { get; set; }

    public Dictionary<string, string?>? Data { get; set; }
}