using System.ComponentModel;

namespace Planar.Common
{
    public enum MonitorEvents
    {
        [Description("Execution Vetoed")] ExecutionVetoed = 100,
        [Description("Execution Retry")] ExecutionRetry = 101,
        [Description("Execution Last Retry Fail")] ExecutionLastRetryFail = 102,
        [Description("Execution Fail")] ExecutionFail = 103,
        [Description("Execution Success")] ExecutionSuccess = 104,
        [Description("Execution Start")] ExecutionStart = 105,
        [Description("Execution End")] ExecutionEnd = 106,
        [Description("Execution Success With No Effected Rows")] ExecutionSuccessWithNoEffectedRows = 107,
        [Description("Execution Progress Changed")] ExecutionProgressChanged = 108,
        [Description("Execution Timeout")] ExecutionTimeout = 109,

        //// - Events with argument --------------------------------
        [Description("Execution Fail {x} Times In Row")] ExecutionFailxTimesInRow = 200,

        [Description("Execution Fail {x} Times In {y} Hours")] ExecutionFailxTimesInyHours = 201,
        [Description("Execution End With Effected Rows Greater Than {x}")] ExecutionEndWithEffectedRowsGreaterThanx = 202,
        [Description("Execution End With Effected Rows Less Than {x}")] ExecutionEndWithEffectedRowsLessThanx = 203,
        [Description("Execution End With Effected Rows Greater Than {x} In {y} Hours")] ExecutionEndWithEffectedRowsGreaterThanxInyHours = 204,
        [Description("Execution End With Effected Rows Less Than {x} In {y} Hours")] ExecutionEndWithEffectedRowsLessThanxInyHours = 205,
        [Description("Execution Duration Greater Than {x} Minutes")] ExecutionDurationGreaterThanxMinutes = 206,

        //// -------------------------------------------------------
        [Description("Job Added")] JobAdded = 300,

        [Description("Job Deleted")] JobDeleted = 301,
        [Description("Job Canceled")] JobCanceled = 302,
        [Description("Job Paused")] JobPaused = 303,
        [Description("Job Resumed")] JobResumed = 304,

        // [Description("Job Group Paused")] JobGroupPaused = 305, ==> REMOVED!
        // [Description("Job Group Resumed")] JobGroupResumed = 306, ==> REMOVED!
        [Description("Scheduler Error")] SchedulerError = 307,

        [Description("Scheduler In Standby Mode")] SchedulerInStandbyMode = 308,
        [Description("Scheduler Started")] SchedulerStarted = 309,
        [Description("Scheduler Shutdown")] SchedulerShutdown = 310,
        [Description("Trigger Paused")] TriggerPaused = 311,
        [Description("Trigger Resumed")] TriggerResumed = 312,
        [Description("Cluster Node Join")] ClusterNodeJoin = 313,
        [Description("Cluster Node Removed")] ClusterNodeRemoved = 314,
        [Description("Cluster Health Check Fail")] ClusterHealthCheckFail = 315,
        [Description("Job Updated")] JobUpdated = 316,
    }
}