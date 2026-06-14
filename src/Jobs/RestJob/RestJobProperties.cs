using CommonJob;
using YamlDotNet.Serialization;

namespace Planar;

public class RestJobBasicAuthentication
{
    [YamlMember(Alias = "password", Order = 0)]
    public string Password { get; set; } = null!;

    [YamlMember(Alias = "username", Order = 1)]
    public string Username { get; set; } = null!;
}

public class RestJobJwtAuthentication
{
    [YamlMember(Alias = "token", Order = 0)]
    public string Token { get; set; } = null!;
}

public class RestJobProperties : IPathJobProperties, IJobPropertiesWithFiles
{
    [YamlMember(Alias = "basic authentication", Order = 0)]
    public RestJobBasicAuthentication? BasicAuthentication { get; set; }

    [YamlMember(Alias = "body file", Order = 1)]
    public string? BodyFile { get; set; }

    [YamlMember(Alias = "expect 100 continue", Order = 2)]
    public bool Expect100Continue { get; set; }

    [YamlMember(Alias = "follow redirects", Order = 3)]
    public bool FollowRedirects { get; set; }

    [YamlMember(Alias = "form data", Order = 4)]
    public Dictionary<string, string>? FormData { get; set; } = new();

    [YamlMember(Alias = "headers", Order = 5)]
    public Dictionary<string, string>? Headers { get; set; } = new();

    [YamlMember(Alias = "ignore ssl errors", Order = 6)]
    public bool IgnoreSslErrors { get; set; }

    [YamlMember(Alias = "jwt authentication", Order = 7)]
    public RestJobJwtAuthentication? JwtAuthentication { get; set; }

    [YamlMember(Alias = "max redirects", Order = 8)]
    public int? MaxRedirects { get; set; }

    [YamlMember(Alias = "method", Order = 9)]
    public string Method { get; set; } = null!;

    [YamlMember(Alias = "path", Order = 10)]
    public string Path { get; set; } = string.Empty;

    [YamlMember(Alias = "proxy", Order = 11)]
    public RestJobPropertiesProxy? Proxy { get; set; }
    
    [YamlMember(Alias = "url", Order = 12)]
    public string Url { get; set; } = null!;

    [YamlMember(Alias = "user agent", Order = 13)]
    public string? UserAgent { get; set; }

    [YamlMember(Alias = "log response content", Order = 14)]
    public bool LogResponseContent { get; set; }

    public IEnumerable<string> Files
    {
        get
        {
            if (string.IsNullOrWhiteSpace(BodyFile)) { return []; }
            return
            [
                string.IsNullOrWhiteSpace(Path) ? BodyFile : System.IO.Path.Combine(Path, BodyFile)
            ];
        }
    }
}

public class RestJobPropertiesNetworkCredential
{
    [YamlMember(Alias = "domain", Order = 0)]
    public string? Domain { get; set; }

    [YamlMember(Alias = "password", Order = 1)]
    public string Password { get; set; } = null!;

    [YamlMember(Alias = "username", Order = 2)]
    public string Username { get; set; } = null!;
}

public class RestJobPropertiesProxy
{
    [YamlMember(Alias = "address", Order = 0)]
    public string Address { get; set; } = null!;

    [YamlMember(Alias = "bypass on local", Order = 1)]
    public bool BypassOnLocal { get; set; }

    [YamlMember(Alias = "use default credentials", Order = 2)]
    public bool UseDefaultCredentials { get; set; }

    [YamlMember(Alias = "credentials", Order = 3)]
    public RestJobPropertiesNetworkCredential? Credentials { get; set; }
}