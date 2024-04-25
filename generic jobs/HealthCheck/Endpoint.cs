using Common;
using Microsoft.Extensions.Configuration;

namespace HealthCheck;

internal class Endpoint(IConfigurationSection section) : BaseDefault(section), IEndpoint, INamedCheckElement
{
    private readonly IConfigurationSection section = section;
    public string Name { get; set; } = section.GetValue<string?>("name") ?? string.Empty;
    public string Url { get; private set; } = section.GetValue<string?>("url") ?? string.Empty;
    public IEnumerable<int>? SuccessStatusCodes { get; set; } = section.GetSection("success status codes").Get<int[]?>();
    public TimeSpan? Timeout { get; set; } = section.GetValue<TimeSpan?>("timeout");
    public int? Port => section.GetValue<int?>("port");
    public bool Active { get; private set; } = section.GetValue<bool?>("active") ?? true;
    public string Key => Url;
}