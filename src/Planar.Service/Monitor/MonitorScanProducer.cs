using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Planar.Service.Monitor;

public class MonitorScanProducer(Channel<MonitorScanMessage> channel, ILogger<MonitorScanProducer> logger)
{
    #region Lock Event

    private static readonly ConcurrentDictionary<string, DateTimeOffset> _lockJobEvents = new();

    public static void LockJobOrTriggerEvent(string? key, int? lockSeconds)
    {
        if (string.IsNullOrWhiteSpace(key) || lockSeconds.GetValueOrDefault() <= 0) { return; }
        _lockJobEvents.TryAdd(key, DateTimeOffset.Now);
        _ = Task.Run(() =>
        {
            Thread.Sleep(lockSeconds.GetValueOrDefault() * 1000);
            _lockJobEvents.TryRemove(key, out _);
        });
    }

    private static bool IsJobOrTriggerEventLock(MonitorScanMessage message)
    {
        if (message == null) { return false; }

        if (message.MonitorSystemInfo != null)
        {
            var info = message.MonitorSystemInfo;
            var @event = message.Event;

            var keyString = string.Empty;
            var hasGroup = info.MessagesParameters.TryGetValue("JobGroup", out var jobGroup);
            var hasName = info.MessagesParameters.TryGetValue("JobName", out var jobName);
            if (hasGroup && hasName)
            {
                keyString = $"{jobGroup}.{jobName} {@event}";
            }

            hasGroup = info.MessagesParameters.TryGetValue("TriggerGroup", out var triggerGroup);
            hasName = info.MessagesParameters.TryGetValue("TriggerName", out var triggerName);
            if (hasGroup && hasName)
            {
                keyString = $"{triggerGroup}.{triggerName} {@event}";
            }

            if (string.IsNullOrEmpty(keyString))
            {
                return false;
            }

            return _lockJobEvents.ContainsKey(keyString);
        }
        else if (message.JobExecutionContext != null)
        {
            var details = message.JobExecutionContext.JobDetail;
            var keyString = $"{details.Key} {message.Event}";
            var result = _lockJobEvents.ContainsKey(keyString);
            if (result)
            {
                // Release
                _lockJobEvents.TryRemove(keyString, out _);
            }
        }

        return false;
    }

    #endregion Lock Event

    public void Publish(MonitorScanMessage message, CancellationToken cancellationToken)
    {
        _ = SafePublishInner(message, cancellationToken);
    }

    public async Task PublishAsync(MonitorScanMessage message, CancellationToken cancellationToken)
    {
        await SafePublishInner(message, cancellationToken);
    }

    private async Task SafePublishInner(MonitorScanMessage message, CancellationToken cancellationToken)
    {
        try
        {
            if (IsJobOrTriggerEventLock(message)) { return; }

            while (await channel.Writer.WaitToWriteAsync(cancellationToken).ConfigureAwait(false))
            {
                if (channel.Writer.TryWrite(message)) { break; }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "fail to publish monitor scan message. the message: {@Message}", message);
        }

        try
        {
            if (message.Type != MonitorScanType.ScanSystem) { return; }
            if (message.MonitorSystemInfo == null) { return; }
            var message2 = new MonitorScanMessage(Common.MonitorEvents.AnySystemEvent, message.MonitorSystemInfo, message.Exception);
            while (await channel.Writer.WaitToWriteAsync(cancellationToken).ConfigureAwait(false))
            {
                await Console.Out.WriteLineAsync($"-Producer-> {message2.Type} {message2.MonitorSystemInfo?.MessageTemplate}");
                if (channel.Writer.TryWrite(message2)) { break; }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "fail to publish monitor scan message. the message: {@Message}", message);
        }
    }
}