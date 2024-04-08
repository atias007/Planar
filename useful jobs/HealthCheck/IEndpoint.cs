namespace HealthCheck;

internal interface IEndpoint
{
    IEnumerable<int>? SuccessStatusCodes { get; set; }
    int? RetryCount { get; set; }
    int? MaximumFailsInRow { get; set; }
    TimeSpan? Timeout { get; set; }
    TimeSpan? RetryInterval { get; set; }
}