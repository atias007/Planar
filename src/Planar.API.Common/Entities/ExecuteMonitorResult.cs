namespace Planar.API.Common.Entities
{
    public class ExecuteMonitorResult
    {
        public bool Success { get; private set; }
        public string? Failure { get; private set; }

        public static ExecuteMonitorResult Ok => new() { Success = true };

        public static ExecuteMonitorResult Fail(string failure) => new() { Success = true, Failure = failure };
    }
}