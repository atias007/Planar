namespace Planar.API.Common.Entities
{
    public enum MonitorEvents
    {
        ExecutionVetoed = 100,
        ExecutionRetry = 101,
        ExecutionLastRetryFail = 102,
        ExecutionFail = 103,
        ExecutionSuccess = 104,
        ExecutionStart = 105,
        ExecutionEnd = 106,
        ExecutionSuccessWithNoEffectedRows = 107,

        //// - Events with argument --------------------------------
        ExecutionFailxTimesInRow = 200,

        ExecutionFailxTimesInHour = 201,
        ExecutionFailWithEffectedRowsGreaterThanx = 202,
        ExecutionFailWithEffectedRowsLessThanx = 203,

        //// -------------------------------------------------------
        JobAdded = 300,

        JobDeleted = 301,
        JobInterrupted = 302,
        JobPaused = 303,
        JobResumed = 304,
        JobGroupPaused = 305,
        JobGroupResumed = 306,
        SchedulerError = 307,
        SchedulerInStandbyMode = 308,
        SchedulerStarted = 309,
        SchedulerShutdown = 310,
        TriggerPaused = 311,
        TriggerResumed = 312,
        ClusterNodeJoin = 313,
        ClusterNodeRemoved = 314
    }
}