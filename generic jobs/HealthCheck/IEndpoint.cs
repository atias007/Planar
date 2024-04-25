namespace HealthCheck;

internal interface IEndpoint
{
    IEnumerable<int>? SuccessStatusCodes { get; }
    int? RetryCount { get; }
    int? MaximumFailsInRow { get; }
    TimeSpan? Timeout { get; }
    TimeSpan? RetryInterval { get; }
}