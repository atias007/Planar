using CommonJob;
using YamlDotNet.Serialization;

namespace Planar
{
    public class RestJobBasicAuthentication
    {
        public string Password { get; set; } = null!;
        public string Username { get; set; } = null!;
    }

    public class RestJobJwtAuthentication
    {
        public string Token { get; set; } = null!;
    }

    public class RestJobProperties : IPathJobProperties
    {
        [YamlMember(Alias = "basic authentication")]
        public RestJobBasicAuthentication? BasicAuthentication { get; set; }

        [YamlMember(Alias = "body file")]
        public string? BodyFile { get; set; }

        [YamlMember(Alias = "expect 100 continue")]
        public bool Expect100Continue { get; set; }

        [YamlMember(Alias = "follow redirects")]
        public bool FollowRedirects { get; set; }

        [YamlMember(Alias = "form data")]
        public Dictionary<string, string>? FormData { get; set; } = new();

        public Dictionary<string, string>? Headers { get; set; } = new();

        [YamlMember(Alias = "ignore ssl errors")]
        public bool IgnoreSslErrors { get; set; }

        [YamlMember(Alias = "jwt authentication")]
        public RestJobJwtAuthentication? JwtAuthentication { get; set; }

        [YamlMember(Alias = "max redirects")]
        public int? MaxRedirects { get; set; }

        public string Method { get; set; } = null!;

        public string Path { get; set; } = null!;

        public RestJobPropertiesProxy? Proxy { get; set; }

        public string Url { get; set; } = null!;

        [YamlMember(Alias = "user agent")]
        public string? UserAgent { get; set; }

        [YamlMember(Alias = "log response content")]
        public bool LogResponseContent { get; set; }
    }

    public class RestJobPropertiesNetworkCredential
    {
        public string? Domain { get; set; }
        public string Password { get; set; } = null!;
        public string Username { get; set; } = null!;
    }

    public class RestJobPropertiesProxy
    {
        public string Address { get; set; } = null!;

        [YamlMember(Alias = "bypass on local")]
        public bool BypassOnLocal { get; set; }

        [YamlMember(Alias = "use default credentials")]
        public bool UseDefaultCredentials { get; set; }

        public RestJobPropertiesNetworkCredential? Credentials { get; set; }
    }
}