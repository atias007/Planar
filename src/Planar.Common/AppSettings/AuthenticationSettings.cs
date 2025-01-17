using Microsoft.IdentityModel.Tokens;
using System;
using YamlDotNet.Serialization;

namespace Planar.Common
{
    public class AuthenticationSettings
    {
        private const string AuthenticationIssuerValue = "https://planar.me";
        private const string AuthenticationAudienceValue = "https://planar.audience.me";

        public AuthMode Mode { get; set; }

        public string? Secret { get; set; }

        public TimeSpan TokenExpire { get; set; }

        public bool ApiSecurityHeaders { get; set; }

        [YamlIgnore]
        public SymmetricSecurityKey Key { get; set; } = null!;

        [YamlIgnore]
        public bool HasAuthontication => Mode != AuthMode.AllAnonymous;

        [YamlIgnore]
        public bool NoAuthontication => Mode == AuthMode.AllAnonymous;

        [YamlIgnore]
        public static string AuthenticationIssuer => AuthenticationIssuerValue;

        [YamlIgnore]
        public static string AuthenticationAudience => AuthenticationAudienceValue;
    }
}