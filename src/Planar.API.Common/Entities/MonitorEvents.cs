namespace Planar.API.Common.Entities
{
    public enum MonitorEvents
    {
        ExecutionVetoed = 1,
        ExecutionRetry = 2,
        ExecutionFail = 3,
        ExecutionSuccess = 4,
        ExecutionStart = 5,
        ExecutionEnd = 6,
        ExecutionSuccessWithNoEffectedRows = 7,

        //// - Events with argument --------------------------------
        ExecutionFailxTimesInRow = 100,

        ExecutionFailxTimesInHour = 101,
        ExecutionFailWithEffectedRowsGreaterThanx = 102,
        ExecutionFailWithEffectedRowsLessThanx = 103,

        //// -------------------------------------------------------
        JobAdded = 200,

        JobDeleted = 201,
        JobInterrupted = 202,
        JobPaused = 203,
        JobResumed = 204,
        JobGroupPaused = 205,
        JobGroupResumed = 206,
        SchedulerError = 207,
        SchedulerInStandbyMode = 208,
        SchedulerStarted = 209,
        SchedulerShuttingdown = 210,
        SchedulerShutdown = 211,
        TriggerPaused = 212,
        TriggerResumed = 213
    }
}