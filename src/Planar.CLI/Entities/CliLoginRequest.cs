using Planar.CLI.Attributes;

namespace Planar.CLI.Entities;

public class CliLoginRequest
{
    [ActionProperty(DefaultOrder = 1)]
    public string Host { get; set; } = string.Empty;

    [ActionProperty(DefaultOrder = 2)]
    public int Port { get; set; }

    [ActionProperty(LongName = "secure", ShortName = "s")]
    public bool SecureProtocol { get; set; }

    [ActionProperty(LongName = "username", ShortName = "u")]
    public string? Username { get; set; }

    [ActionProperty(LongName = "password", ShortName = "p")]
    public string? Password { get; set; }

    [ActionProperty("c", "color")]
    public CliColors Color { get; set; }

    ////[IterativeActionProperty]
    ////public bool Iterative { get; set; }

    internal string Key => $"{Host}:{Port}";
}