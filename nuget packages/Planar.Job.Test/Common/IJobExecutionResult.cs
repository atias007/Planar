using System;
using System.Collections.Generic;

namespace Planar.Job.Test
{
    public interface IJobExecutionResult
    {
        int Id { get; }
        string InstanceId { get; }
        string JobId { get; }
        string JobName { get; }
        string JobGroup { get; }
        string TriggerId { get; }
        string TriggerName { get; }
        string TriggerGroup { get; }
        StatusMembers Status { get; }
        DateTime StartDate { get; }
        DateTime? EndDate { get; }
        int? Duration { get; }
        int? EffectedRows { get; }
        bool Retry { get; }
        bool IsCanceled { get; }
        IReadOnlyDictionary<string, string?> Data { get; }
        string? Log { get; }
        Exception? Exception { get; }
        AssertPlanarConstraint Assert { get; }
        object? Instance { get; }
    }
}