using Microsoft.Extensions.Configuration;

namespace HealthCheck;

internal class Endpoint(IConfigurationSection section, string url) : IEndpoint
{
    public string? Name { get; set; } = section.GetValue<string?>("name");
    public string Url { get; private set; } = url;
    public IEnumerable<int>? SuccessStatusCodes { get; set; } = section.GetSection("success status codes").Get<int[]?>();
    public TimeSpan? Timeout { get; set; } = section.GetValue<TimeSpan?>("timeout");
    public int? RetryCount { get; set; } = section.GetValue<int?>("retry count");
    public TimeSpan? RetryInterval { get; set; } = section.GetValue<TimeSpan?>("retry interval");
    public int? MaximumFailsInRow { get; set; } = section.GetValue<int?>("maximum fails in row");
}