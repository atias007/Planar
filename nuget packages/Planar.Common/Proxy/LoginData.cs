using System;

namespace Planar.Common.Proxy
{
    internal struct LoginData
    {
        public bool SecureProtocol { get; set; }
        public int Port { get; set; }
#if NETSTANDARD2_0
        public string Host { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
#else
        public string? Host { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
#endif

        public TimeSpan? Timeout { get; set; }
    }
}