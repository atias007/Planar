using Common;
using Microsoft.Extensions.Configuration;

namespace HealthCheck;

internal class Endpoint : BaseDefault, IEndpoint, INamedCheckElement, IVetoEntity
{
    public Endpoint(Endpoint source) : base(source)
    {
        Name = source.Name;
        HostGroupName = source.HostGroupName;
        Url = source.Url;
        SuccessStatusCodes = source.SuccessStatusCodes;
        Timeout = source.Timeout;
        Port = source.Port;
        AbsoluteUrl = source.AbsoluteUrl;
    }

    public Endpoint(IConfigurationSection section, Defaults defaults) : base(section, defaults)
    {
        Name = section.GetValue<string?>("name") ?? string.Empty;
        HostGroupName = section.GetValue<string?>("host group name");
        Url = section.GetValue<string?>("url") ?? string.Empty;
        SuccessStatusCodes = section.GetSection("success status codes").Get<int[]?>() ?? defaults.SuccessStatusCodes;
        Timeout = section.GetValue<TimeSpan?>("timeout") ?? defaults.Timeout;
        Port = section.GetValue<int?>("port");
        AbsoluteUrl = SetAbsoluteUrl(Url);
    }

    public string Name { get; }
    public string? HostGroupName { get; private set; }
    public string Url { get; }
    public IEnumerable<int> SuccessStatusCodes { get; }
    public TimeSpan Timeout { get; }
    public int? Port { get; }
    public string Key => Host == null ? Name : $"{Name} ({Host})";
    public Uri? AbsoluteUrl { get; }
    public bool IsAbsoluteUrl => !IsRelativeUrl;
    public bool IsRelativeUrl => AbsoluteUrl == null;

    // internal use for relative urls
    public Uri? Host { get; set; }

    //// -------------------------- ////

    public EndpointResult Result { get; } = new();

    private static Uri? SetAbsoluteUrl(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var result)) { return result; }
        return null;
    }
}