using System;

namespace Planar.Common.PeriodicalBatch;

public class PeriodicalBatchOptionsBuilder<TMessage>
        where TMessage : class
{
    private readonly PeriodicalBatchOptions<TMessage> _options = new();

    internal PeriodicalBatchOptionsBuilder()
    {
    }

    public PeriodicalBatchOptionsBuilder<TMessage> WithBatchSize(int batchSize)
    {
        _options.BatchSize = batchSize;
        return this;
    }

    public PeriodicalBatchOptionsBuilder<TMessage> WithPeriod(TimeSpan period)
    {
        _options.Period = period;
        return this;
    }

    public PeriodicalBatchOptionsBuilder<TMessage> WithoutRetry()
    {
        _options.Retry = false;
        return this;
    }

    public PeriodicalBatchOptionsBuilder<TMessage> WithRetryCount(int retryCount)
    {
        _options.RetryCount = retryCount;
        return this;
    }

    public PeriodicalBatchOptionsBuilder<TMessage> WithHealthCheck(TimeSpan interval)
    {
        _options.HealthCheckInterval = interval;
        return this;
    }

    internal PeriodicalBatchOptions<TMessage> Build()
    {
        return _options;
    }
}