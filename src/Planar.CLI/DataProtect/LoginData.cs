using Planar.CLI.Entities;
using System;

namespace Planar.CLI.DataProtect;

public class LoginData
{
    public required string DisplayName { get; set; }
    public required string Host { get; set; }
    public required int Port { get; set; }
    public bool SecureProtocol { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? Token { get; set; }
    public DateTime? Expire { get; set; }
    public CliColors Color { get; set; }
    public string Key => $"{Host}:{Port}";
    public bool Deprecated => Expire < DateTime.Now;
    public bool HasCredentials => !(string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password));
}