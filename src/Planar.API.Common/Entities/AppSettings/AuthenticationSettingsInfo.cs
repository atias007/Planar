using System;

namespace Planar.API.Common.Entities
{
    public class AuthenticationSettingsInfo
    {
        public string? Mode { get; set; }

        public TimeSpan TokenExpire { get; set; }
    }
}