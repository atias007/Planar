using Quartz;
using System.Collections.Concurrent;

namespace CommonJob;

public interface ISequenceInstance
{
    bool SignalEvent(JobKey stepJobKey, string fireInstanceId, string sequenceFireInstanceId, SequenceJobStepEvent @event);
}

public static class SequenceManager
{
    private static readonly ConcurrentDictionary<string, ISequenceInstance> _sequences = [];

    public static void RegisterSequence(string instanceId, ISequenceInstance sequence)
    {
        _sequences.TryAdd(instanceId, sequence);
    }

    public static bool SignalEvent(JobKey stepJobKey, string fireInstanceId, string sequenceFireInstanceId, SequenceJobStepEvent @event)
    {
        if (_sequences.TryGetValue(sequenceFireInstanceId, out var sequence))
        {
            return sequence.SignalEvent(stepJobKey, fireInstanceId, sequenceFireInstanceId, @event);
        }

        return false;
    }

    public static void UnregisterSequence(string instanceId)
    {
        _sequences.TryRemove(instanceId, out _);
    }
}