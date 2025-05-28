namespace Planar.CLI.Proxy;

internal class LoginData
{
    public bool SecureProtocol { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
}