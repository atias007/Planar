using Planar.Job.Test.Common;
using System;

namespace Planar.Job.Test
{
    internal class JobExecutionResult : IJobExecutionResult
    {
        public int Id { get; set; }
        public string InstanceId { get; set; }
        public string JobId { get; set; }
        public string JobName { get; set; }
        public string JobGroup { get; set; }
        public string TriggerId { get; set; }
        public string TriggerName { get; set; }
        public string TriggerGroup { get; set; }
        public StatusMembers Status { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? Duration { get; set; }
        public int? EffectedRows { get; set; }
        public bool Retry { get; set; }
        public bool IsStopped { get; set; }
        public string Data { get; set; }
        public string Log { get; set; }
        public Exception Exception { get; set; }

        public void AssertFail()
        {
            if (Status == StatusMembers.Fail) { return; }
            var message = $"Expect status {StatusMembers.Fail} but status was {Status}";
            throw new AssertPlanarException(message);
        }

        public void AssertSuccess()
        {
            if (Status == StatusMembers.Success) { return; }

            var message = $"Expect status {StatusMembers.Success} but status was {Status}";
            if (Exception != null)
            {
                message += "Exception:\r\n{Exception}";
            }

            throw new AssertPlanarException(message);
        }
    }
}