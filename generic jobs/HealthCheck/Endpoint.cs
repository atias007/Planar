using Common;
using Microsoft.Extensions.Configuration;

namespace HealthCheck;

internal class Endpoint(IConfigurationSection section, string url) : BaseDefault(section), IEndpoint, ICheckElemnt
{
    public string Name { get; set; } = section.GetValue<string?>("name") ?? string.Empty;
    public string Url { get; private set; } = url;
    public IEnumerable<int>? SuccessStatusCodes { get; set; } = section.GetSection("success status codes").Get<int[]?>();
    public TimeSpan? Timeout { get; set; } = section.GetValue<TimeSpan?>("timeout");

    public string Key => Url;
}