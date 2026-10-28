#nullable enable

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly.Retry;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Planar.Common.PeriodicalBatch;

public abstract class PeriodicalBatch<TMessage>(IServiceProvider serviceProvider) : BackgroundService
    where TMessage : class
{
    private int _locker;
    private readonly ConcurrentQueue<TMessage> _queue = new();
    private readonly ConcurrentQueue<MessageWrapper> _errorQueue = new();
    private readonly Channel<TMessage> _channel = serviceProvider.GetRequiredService<Channel<TMessage>>();
    private readonly ILogger<PeriodicalBatch<TMessage>> _logger = serviceProvider.GetRequiredService<ILogger<PeriodicalBatch<TMessage>>>();
    private readonly PeriodicalBatchOptions<TMessage> _options = serviceProvider.GetRequiredService<PeriodicalBatchOptions<TMessage>>();
    protected IServiceProvider ServiceProvider => serviceProvider;

    public async override Task StopAsync(CancellationToken cancellationToken)
    {
        await FlushQueue().ConfigureAwait(false);
        await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _ = StartHandleQueueRoutine(stoppingToken);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Initialize periodical batch service {Name} (Batch Size: {BatchSize}, Period: {Period}, Retry: {Retry}, Retry Count: {RetryCount})",
                GetType().Name,
                _options.BatchSize,
                _options.Period,
                _options.Retry,
                _options.RetryCount);
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

    private async Task StartHandleQueueRoutine(CancellationToken stoppingToken)
    {
        using var periodicTimer = new PeriodicTimer(_options.Period);
        while (await periodicTimer.WaitForNextTickAsync(stoppingToken))
        {
            await SafeHandleQueue().ConfigureAwait(false);
            await SafeHandleErrorQueue().ConfigureAwait(false);
        }
    }

    private async Task CheckQueueSize()
    {
        if (_queue.Count >= _options.BatchSize)
        {
            await SafeHandleQueue().ConfigureAwait(false);
            await SafeHandleErrorQueue().ConfigureAwait(false);
        }
    }

    private async Task SafeHandleQueue()
    {
        if (0 != Interlocked.Exchange(ref _locker, 1)) { return; } // acquired the lock

        try
        {
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
        }
    }

    private async Task SafeHandleErrorQueue()
    {
        if (0 != Interlocked.Exchange(ref _locker, 1)) { return; } // acquired the lock

        try
        {
            do
            {
                await HandleErrorBatch().ConfigureAwait(false);
            }

            while (_errorQueue.Count > _options.BatchSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fail to handle PeriodicalBatch queue ({Name})", GetType().FullName);
        }
        finally
        {
            // Release the lock
            Interlocked.Exchange(ref _locker, 0);
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
            await HandleBatch(chunk).ConfigureAwait(false);
            _logger.LogDebug("Flush {Count} items from periodical batch queue ({Name})", chunk.Count, GetType().FullName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fail to handle FlushQueue ({Name})", GetType().FullName);
        }

        var error_chunk = _errorQueue.ToList();
        try
        {
            await HandleBatch(error_chunk.Select(e => e.Message)).ConfigureAwait(false);
            _logger.LogDebug("Flush {Count} items from periodical batch queue ({Name})", error_chunk.Count, GetType().FullName);
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
            await HandleBatch(chunk).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fail to handle PeriodicalBatch batch ({Name})", GetType().FullName);
            if (_options.Retry && _options.RetryCount > 0)
            {
                EnqueueChunk(chunk, retryCount: 1);
            }
        }
    }

    private async Task HandleErrorBatch()
    {
        if (_errorQueue.IsEmpty)
        {
            return;
        }

        var chunk = new List<MessageWrapper>(_options.BatchSize);
        for (var i = 0; i < _options.BatchSize; i++)
        {
            if (!_errorQueue.TryDequeue(out var item))
            {
                break;
            }

            chunk.Add(item);
        }

        try
        {
            var items = chunk.Select(x => x.Message).ToList();
            await HandleBatch(items).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fail to handle PeriodicalBatch batch ({Name})", GetType().FullName);
            if (_options.Retry)
            {
                EnqueueChunk(chunk);
            }
        }
    }

    private void EnqueueChunk(IEnumerable<TMessage> items, int retryCount)
    {
        foreach (var item in items)
        {
            try
            {
                _errorQueue.Enqueue(new MessageWrapper { Message = item, RetryCount = retryCount });
            }
            catch
            {
                // *** DO NOTHING ** //
            }
        }
    }

    private void EnqueueChunk(IEnumerable<MessageWrapper> items)
    {
        foreach (var item in items)
        {
            try
            {
                if (item.RetryCount >= _options.RetryCount)
                {
                    continue;
                }

                _errorQueue.Enqueue(new MessageWrapper { Message = item.Message, RetryCount = item.RetryCount + 1 });
            }
            catch
            {
                // *** DO NOTHING ** //
            }
        }
    }

    protected abstract Task HandleBatch(IEnumerable<TMessage> items);

    protected abstract Task HealthCheck();

    private sealed class MessageWrapper
    {
        public TMessage Message { get; set; } = null!;
        public int RetryCount { get; set; }
    }
}