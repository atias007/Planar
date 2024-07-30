using Common;
using Microsoft.Extensions.Configuration;

namespace HealthCheck;

internal class Endpoint : BaseDefault, IEndpoint, INamedCheckElement
{
    public Endpoint(IConfigurationSection section, Defaults defaults) : base(section, defaults)
    {
        Name = section.GetValue<string?>("name") ?? string.Empty;
        Url = section.GetValue<string?>("url") ?? string.Empty;
        SuccessStatusCodes = section.GetSection("success status codes").Get<int[]?>() ?? defaults.SuccessStatusCodes;
        Timeout = section.GetValue<TimeSpan?>("timeout") ?? defaults.Timeout;
        Port = section.GetValue<int?>("port");
        Active = section.GetValue<bool?>("active") ?? true;
        AbsoluteUrl = SetAbsoluteUrl(Url);
    }

    private Endpoint(Endpoint endpoint)
    {
        Name = endpoint.Name;
        Url = endpoint.Url;
        SuccessStatusCodes = endpoint.SuccessStatusCodes;
        Timeout = endpoint.Timeout;
        Port = endpoint.Port;
        Active = endpoint.Active;
        AbsoluteUrl = endpoint.AbsoluteUrl;
        RetryCount = endpoint.RetryCount;
        RetryInterval = endpoint.RetryInterval;
        MaximumFailsInRow = endpoint.MaximumFailsInRow;
        Span = endpoint.Span;
    }

    public Endpoint Clone()
    {
        return new(this);
    }

    public string Name { get; }
    public string Url { get; }
    public IEnumerable<int> SuccessStatusCodes { get; }
    public TimeSpan Timeout { get; }
    public int? Port { get; }
    public bool Active { get; }
    public string Key => Name;
    public Uri? AbsoluteUrl { get; }
    public bool IsAbsoluteUrl => !IsRelativeUrl;
    public bool IsRelativeUrl => AbsoluteUrl == null;

    // internal use for relative urls
    public Uri? Host { get; set; }

    private static Uri? SetAbsoluteUrl(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var result)) { return result; }
        return null;
    }
}