using System;

namespace Planar.PeriodicalBatch;

public class PeriodicalBatchOptions<TMessage>
        where TMessage : class
{
    public int BatchSize { get; internal set; } = 300;
    public TimeSpan Period { get; internal set; } = TimeSpan.FromSeconds(3);
    public bool Retry { get; internal set; } = true;
    public int RetryCount { get; internal set; } = 3;
    public TimeSpan? HealthCheckInterval { get; internal set; }
    internal static PeriodicalBatchOptions<TMessage> Empty => new();
}