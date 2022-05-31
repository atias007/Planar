namespace Planar
{
    public enum MonitorEvents
    {
        ExecutionVetoed = 1,
        ExecutionRetry = 2,
        ExecutionFail = 3,
        ExecutionSuccess = 4,
        ExecutionStart = 5,
        ExecutionEnd = 6,
        ExecutionFailnTimesInRow = 10,
        ExecutionFailnTimesInHour = 11
    }
}