namespace RestHealthCheck;

internal class Defaults : IEndpoint
{
    public IEnumerable<int>? SuccessStatusCodes { get; set; } = new List<int> { 200, 201, 202, 204 };
    public int? RetryCount { get; set; } = 3;
    public TimeSpan? Timeout { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan? RetryInterval { get; set; } = TimeSpan.FromSeconds(5);

    public static Defaults Empty => new();
}