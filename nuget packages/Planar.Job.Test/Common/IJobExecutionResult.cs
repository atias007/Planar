using System;

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
        bool IsStopped { get; }
        string Data { get; }
        string Log { get; }
        Exception Exception { get; }

        void AssertSuccess();

        void AssertFail();
    }
}