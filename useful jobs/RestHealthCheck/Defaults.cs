namespace RestHealthCheck;

internal class Defaults
{
    public IEnumerable<int>? SuccessStatusCodes { get; set; }
    public int? RetryCount { get; set; }
    public TimeSpan? Timeout { get; set; }
    public TimeSpan? RetryInterval { get; set; }
}