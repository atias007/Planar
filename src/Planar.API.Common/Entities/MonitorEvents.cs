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
        ExecutionFailxTimesInRow = 100, // this element must be 100 value. its value is compared in some places in code
        ExecutionFailxTimesInHour = 101,
        ExecutionFailWithEffectedRowsGreaterThanx = 102,
        ExecutionFailWithEffectedRowsLessThanx = 103,
    }
}