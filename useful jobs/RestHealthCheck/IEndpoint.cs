namespace RestHealthCheck;

internal interface IEndpoint
{
    IEnumerable<int>? SuccessStatusCodes { get; set; }
    int? RetryCount { get; set; }
    TimeSpan? Timeout { get; set; }
    TimeSpan? RetryInterval { get; set; }
}