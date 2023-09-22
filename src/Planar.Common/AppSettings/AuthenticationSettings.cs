using Microsoft.IdentityModel.Tokens;
using System;

namespace Planar.Common
{
    public class AuthenticationSettings
    {
        private const string AuthenticationIssuerValue = "http://planar.me";
        private const string AuthenticationAudienceValue = "http://planar.audience.me";

        public AuthMode Mode { get; set; }

        public string? Secret { get; set; }

        public TimeSpan TokenExpire { get; set; }

        public SymmetricSecurityKey Key { get; set; } = null!;

        public bool HasAuthontication => Mode != AuthMode.AllAnonymous;

        public bool NoAuthontication => Mode == AuthMode.AllAnonymous;

        public static string AuthenticationIssuer => AuthenticationIssuerValue;

        public static string AuthenticationAudience => AuthenticationAudienceValue;
    }
}