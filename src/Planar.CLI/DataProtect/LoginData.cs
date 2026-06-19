using Planar.CLI.Entities;
using System;

namespace Planar.CLI.DataProtect
{
    public class LoginData
    {
        public string Host { get; set; } = null!;

        public int Port { get; set; }

        public bool SecureProtocol { get; set; }

        public string? Username { get; set; }

        public string? Password { get; set; }

        public string? Token { get; set; }

        public DateTime Expire { get; set; }

        public CliColors Color { get; set; }

        public string Key => $"{Host}:{Port}";

        public bool Deprecated => Expire < DateTime.Now;

        public bool HasCredentials => !(string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password));
    }
}