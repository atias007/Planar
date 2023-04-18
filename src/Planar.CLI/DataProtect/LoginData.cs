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

        public int? RememberDays { get; set; }

        public bool Remember { get; set; }

        public CliColors Color { get; set; }

        public DateTimeOffset ConnectDate { get; set; }

        public string Key => $"{Host}:{Port}";

        public bool Deprecated => Remember && RememberDays.HasValue && ConnectDate.Date.AddDays(RememberDays.Value + 1) < DateTimeOffset.Now.Date;

        public bool HasCredentials => Remember && !(string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password));
    }
}