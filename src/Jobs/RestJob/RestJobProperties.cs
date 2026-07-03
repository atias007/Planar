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

public class RestJobProperties : BaseProperties, IJobProperties, IPathJobProperties, IJobPropertiesWithFiles
{
    [YamlIgnore]
    public string Path { get; private set; } = null!;

    [YamlMember(Alias = "body file", Order = 0)]
#pragma warning disable CS9264 // Non-nullable property must contain a non-null value when exiting constructor. Consider adding the 'required' modifier, or declaring the property as nullable, or safely handling the case where 'field' is null in the 'get' accessor.
    public string? BodyFile
#pragma warning restore CS9264 // Non-nullable property must contain a non-null value when exiting constructor. Consider adding the 'required' modifier, or declaring the property as nullable, or safely handling the case where 'field' is null in the 'get' accessor.
    {
        get;
        set
        {
            field = value;
            if (string.IsNullOrWhiteSpace(value))
            {
                Path = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Jobs);
            }
            else
            {
                var fullname =
                    System.IO.Path.IsPathFullyQualified(value) ?
                    value :
                    FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Jobs, value);

                var fi = new FileInfo(fullname);
                Path = fi.DirectoryName ?? string.Empty;
            }
        }
    }

    [YamlMember(Alias = "basic authentication", Order = 1)]
    public RestJobBasicAuthentication? BasicAuthentication { get; set; }

    [YamlMember(Alias = "expect 100 continue", Order = 2)]
    public bool Expect100Continue { get; set; }

    [YamlMember(Alias = "follow redirects", Order = 3)]
    public bool FollowRedirects { get; set; }

    [YamlMember(Alias = "form data", Order = 4)]
    public Dictionary<string, string>? FormData { get; set; } = [];

    [YamlMember(Alias = "headers", Order = 5)]
    public Dictionary<string, string>? Headers { get; set; } = [];

    [YamlMember(Alias = "ignore ssl errors", Order = 6)]
    public bool IgnoreSslErrors { get; set; }

    [YamlMember(Alias = "jwt authentication", Order = 7)]
    public RestJobJwtAuthentication? JwtAuthentication { get; set; }

    [YamlMember(Alias = "max redirects", Order = 8)]
    public int? MaxRedirects { get; set; }

    [YamlMember(Alias = "method", Order = 9)]
    public string Method { get; set; } = null!;

    [YamlMember(Alias = "proxy", Order = 10)]
    public RestJobPropertiesProxy? Proxy { get; set; }

    [YamlMember(Alias = "url", Order = 11)]
    public string Url { get; set; } = null!;

    [YamlMember(Alias = "user agent", Order = 12)]
    public string? UserAgent { get; set; }

    [YamlMember(Alias = "log response content", Order = 13)]
    public bool LogResponseContent { get; set; }

    [YamlIgnore]
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

    public void SetGlobalConfigPlaceholder(Dictionary<string, string?> parameters)
    {
        UserAgent = GetGlobalConfigPropertyPlaceholder(() => UserAgent, parameters) ?? UserAgent;

        Proxy?.Address = GetGlobalConfigPropertyPlaceholder(() => Proxy.Address, parameters) ?? Proxy.Address;
        Proxy?.Credentials?.Domain = GetGlobalConfigPropertyPlaceholder(() => Proxy.Credentials.Domain, parameters) ?? Proxy.Credentials.Domain;
        Proxy?.Credentials?.Password = GetGlobalConfigPropertyPlaceholder(() => Proxy.Credentials.Password, parameters) ?? Proxy.Credentials.Password;
        Proxy?.Credentials?.Username = GetGlobalConfigPropertyPlaceholder(() => Proxy.Credentials.Username, parameters) ?? Proxy.Credentials.Username;
        BasicAuthentication?.Password = GetGlobalConfigPropertyPlaceholder(() => BasicAuthentication.Password, parameters) ?? BasicAuthentication.Password;
        BasicAuthentication?.Username = GetGlobalConfigPropertyPlaceholder(() => BasicAuthentication.Username, parameters) ?? BasicAuthentication.Username;
        JwtAuthentication?.Token = GetGlobalConfigPropertyPlaceholder(() => JwtAuthentication.Token, parameters) ?? JwtAuthentication.Token;
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