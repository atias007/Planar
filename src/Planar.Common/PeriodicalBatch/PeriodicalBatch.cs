#nullable enable

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace Planar.Common.PeriodicalBatch;

public abstract class PeriodicalBatch<TMessage>(IServiceProvider serviceProvider) : BackgroundService
    where TMessage : class

{
    private int _locker;
    private Timer _timer = null!;
    private readonly ConcurrentQueue<TMessage> _queue = new();
    private readonly Channel<TMessage> _channel = serviceProvider.GetRequiredService<Channel<TMessage>>();
    private readonly ILogger<PeriodicalBatch<TMessage>> _logger = serviceProvider.GetRequiredService<ILogger<PeriodicalBatch<TMessage>>>();
    private readonly PeriodicalBatchOptions<TMessage> _options = serviceProvider.GetRequiredService<PeriodicalBatchOptions<TMessage>>();
    private AsyncRetryPolicy? _policy;

    protected IServiceProvider ServiceProvider => serviceProvider;

    protected AsyncRetryPolicy RetryPolicy
    {
        get
        {
            _policy ??= Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(_options.RetryCount, retryAttempt => TimeSpan.FromSeconds(0.5 + Math.Pow(2, retryAttempt - 1)));
            return _policy;
        }
    }

    public async override Task StopAsync(CancellationToken cancellationToken)
    {
        await FlushQueue().ConfigureAwait(false);
        await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _timer = new Timer(_options.Period);
        _timer.Elapsed += async (sender, e) => await TimerElapsed();
        _timer.Start();
        _logger.LogDebug("Initialize periodical batch service {Name} (Batch Size: {BatchSize}, Period: {Period}, Retry: {Retry}, Retry Count: {RetryCount})",
            GetType().Name,
            _options.BatchSize,
            _options.Period,
            _options.Retry,
            _options.RetryCount);

        if (_options.HealthCheckInterval.HasValue && _options.HealthCheckInterval > TimeSpan.Zero)
        {
            var healthCheckTimer = new Timer(_options.HealthCheckInterval.Value);
            healthCheckTimer.Elapsed += async (sender, e) => await SafeHealthCheck().ConfigureAwait(false);
            healthCheckTimer.Start();
        }

        var reader = _channel.Reader;
        try
        {
            while (!reader.Completion.IsCompleted && await reader.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
            {
                if (reader.TryRead(out var message))
                {
                    _queue.Enqueue(message);
                    _ = CheckQueueSize();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // *** DO NOTHING ** //
        }

        _channel.Writer.TryComplete();
        await FlushQueue().ConfigureAwait(false);
    }

    private async Task TimerElapsed()
    {
        await SafeHandleQueue().ConfigureAwait(false);
    }

    private async Task CheckQueueSize()
    {
        if (_queue.Count >= _options.BatchSize)
        {
            await SafeHandleQueue().ConfigureAwait(false);
        }
    }

    private async Task SafeHandleQueue()
    {
        try
        {
            _timer?.Stop();
            if (0 != Interlocked.Exchange(ref _locker, 1)) { return; } // acquired the lock
            do
            {
                await HandleBatch().ConfigureAwait(false);
            }

            while (_queue.Count > _options.BatchSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fail to handle PeriodicalBatch queue ({Name})", GetType().FullName);
        }
        finally
        {
            // Release the lock
            Interlocked.Exchange(ref _locker, 0);
            _timer.Start();
        }
    }

    private async Task FlushQueue()
    {
        if (_queue.IsEmpty)
        {
            _logger.LogDebug("No items to flush from periodical batch queue ({Name})", GetType().FullName);
            return;
        }

        var chunk = _queue.ToList();
        try
        {
            await HandleBatchInner(chunk).ConfigureAwait(false);
            _logger.LogDebug("Flush {Count} items from periodical batch queue ({Name})", chunk.Count, GetType().FullName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fail to handle FlushQueue ({Name})", GetType().FullName);
        }
    }

    private async Task HandleBatch()
    {
        if (_queue.IsEmpty)
        {
            return;
        }

        var chunk = new List<TMessage>(_options.BatchSize);
        for (var i = 0; i < _options.BatchSize; i++)
        {
            if (!_queue.TryDequeue(out var item))
            {
                break;
            }

            chunk.Add(item);
        }

        try
        {
            await HandleBatchInner(chunk).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fail to handle PeriodicalBatch batch ({Name})", GetType().FullName);
            EnqueueChunk(chunk);
        }
    }

    private void EnqueueChunk(IEnumerable<TMessage> items)
    {
        foreach (var item in items)
        {
            try
            {
                _queue.Enqueue(item);
            }
            catch
            {
                // *** DO NOTHING ** //
            }
        }
    }

    private async Task HandleBatchInner(IEnumerable<TMessage> items)
    {
        if (_options.Retry)
        {
            await RetryPolicy.ExecuteAsync(() => HandleBatch(items)).ConfigureAwait(false);
        }
        else
        {
            await HandleBatch(items).ConfigureAwait(false);
        }
    }

    private async Task SafeHealthCheck()
    {
        try
        {
            await HealthCheck();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fail to health check PeriodicalBatch ({Name})", GetType().FullName);
        }
    }

    protected abstract Task HandleBatch(IEnumerable<TMessage> items);

    protected abstract Task HealthCheck();
}