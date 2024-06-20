using Common;
using Microsoft.Extensions.Configuration;

namespace HealthCheck;

internal class Endpoint : BaseDefault, IEndpoint, INamedCheckElement
{
    public Endpoint(IConfigurationSection section) : base(section)
    {
        Name = section.GetValue<string?>("name") ?? string.Empty;
        Url = section.GetValue<string?>("url") ?? string.Empty;
        SuccessStatusCodes = section.GetSection("success status codes").Get<int[]?>();
        Timeout = section.GetValue<TimeSpan?>("timeout");
        Port = section.GetValue<int?>("port");
        Active = section.GetValue<bool?>("active") ?? true;
        AbsoluteUrl = SetAbsoluteUrl(Url);
    }

    public string Name { get; }
    public string Url { get; }
    public IEnumerable<int>? SuccessStatusCodes { get; set; }
    public TimeSpan? Timeout { get; set; }
    public int? Port { get; }
    public bool Active { get; }
    public string Key => Url;

    public Uri? AbsoluteUrl { get; }

    public bool IsAbsoluteUrl => !IsRelativeUrl;
    public bool IsRelativeUrl => AbsoluteUrl == null;

    private static Uri? SetAbsoluteUrl(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var result)) { return result; }
        return null;
    }
}