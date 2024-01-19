using System;

namespace Planar.Client
{
    internal struct LoginData
    {
        public string? Host { get; set; }
        public bool SecureProtocol { get; set; }
        public int Port { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public TimeSpan? Timeout { get; set; }
    }
}