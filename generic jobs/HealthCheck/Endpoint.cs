using Common;
using Microsoft.Extensions.Configuration;

namespace HealthCheck;

internal class Endpoint(IConfigurationSection section, string url) : BaseDefault(section), IEndpoint, INamedCheckElement
{
    public string Name { get; set; } = section.GetValue<string?>("name") ?? string.Empty;
    public string Url { get; private set; } = url;
    public IEnumerable<int>? SuccessStatusCodes { get; set; } = section.GetSection("success status codes").Get<int[]?>();
    public TimeSpan? Timeout { get; set; } = section.GetValue<TimeSpan?>("timeout");
    public bool Active { get; private set; } = section.GetValue<bool?>("active") ?? true;
    public string Key => Url;
}