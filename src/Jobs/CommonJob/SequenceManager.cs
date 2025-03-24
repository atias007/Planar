using Polly;
using Polly.Retry;
using Quartz;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace CommonJob;

public interface ISequenceInstance
{
    bool SignalEvent(JobKey stepJobKey, string fireInstanceId, string sequenceFireInstanceId, SequenceJobStepEvent @event);
}

internal struct SignalEventModel(JobKey stepJobKey, string fireInstanceId, string sequenceFireInstanceId, SequenceJobStepEvent @event)
{
    private byte _retryCounter;
    public readonly JobKey StepJobKey => stepJobKey;
    public readonly string FireInstanceId => fireInstanceId;
    public readonly string SequenceFireInstanceId => sequenceFireInstanceId;
    public readonly SequenceJobStepEvent Event => @event;
    public readonly byte RetryCounter => _retryCounter;

    public void IncreaseRetryCounter() => _retryCounter++;
}

public static class SequenceManager
{
    private static readonly ConcurrentDictionary<string, ISequenceInstance> _sequences = [];
    private static readonly ConcurrentQueue<SignalEventModel> _signalEvents = [];
    private static readonly RetryPolicy _retryPolicy = Policy.Handle<Exception>().WaitAndRetry(3, _ => TimeSpan.FromSeconds(1));
    private static readonly SemaphoreSlim _signalEventSemaphore = new(1, 1);

    public static void RegisterSequence(string instanceId, ISequenceInstance sequence)
    {
        _sequences.TryAdd(instanceId, sequence);
    }

    public static async Task<bool> SignalEvent(JobKey stepJobKey, string fireInstanceId, string sequenceFireInstanceId, SequenceJobStepEvent @event)
    {
        try
        {
            if (_sequences.TryGetValue(sequenceFireInstanceId, out var sequence))
            {
                return _retryPolicy.Execute(() => sequence.SignalEvent(stepJobKey, fireInstanceId, sequenceFireInstanceId, @event));
            }

            await Task.Delay(333);
            if (_sequences.TryGetValue(sequenceFireInstanceId, out sequence))
            {
                return _retryPolicy.Execute(() => sequence.SignalEvent(stepJobKey, fireInstanceId, sequenceFireInstanceId, @event));
            }
        }
        catch
        {
            var model = new SignalEventModel(stepJobKey, fireInstanceId, sequenceFireInstanceId, @event);
            _signalEvents.Enqueue(model);
            _ = ProcessSignalEvents();
        }

        return false;
    }

    public static void UnregisterSequence(string instanceId)
    {
        _sequences.TryRemove(instanceId, out _);
    }

    private static async Task ProcessSignalEvents()
    {
        var locked = await _signalEventSemaphore.WaitAsync(0);
        if (!locked) { return; }

        try
        {
            while (!_signalEvents.IsEmpty)
            {
                ProcessSignalEventsInner();
                await Task.Delay(10_000);
            }
        }
        finally
        {
            _signalEventSemaphore.Release();
        }
    }

    private static void ProcessSignalEventsInner()
    {
        for (int i = 0; i < _signalEvents.Count; i++)
        {
            if (!_signalEvents.TryDequeue(out var model)) { break; }

            try
            {
                if (_sequences.TryGetValue(model.SequenceFireInstanceId, out var sequence))
                {
                    _retryPolicy.Execute(() => sequence.SignalEvent(model.StepJobKey, model.FireInstanceId, model.SequenceFireInstanceId, model.Event));
                    continue;
                }
            }
            catch
            {
                //// ***** DO NOTHING ***** ////
            }

            if (model.RetryCounter <= 20)
            {
                model.IncreaseRetryCounter();
                _signalEvents.Enqueue(model);
            }
        }
    }
}