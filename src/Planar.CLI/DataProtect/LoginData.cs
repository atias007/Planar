using Planar.CLI.Entities;
using Spectre.Console;
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
    public string? Filename { get; set; }
    public bool Deprecated => Expire < DateTime.Now;
    public bool HasCredentials => !(string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password));

    public static LoginData Default => new()
    {
        DisplayName = nameof(Default).ToLower(),
        Host = ConnectUtil.DefaultHost,
        Port = ConnectUtil.DefaultPort,
        Color = CliColors.Default
    };

    public string GetCliMarkupColor()
    {
        return Color switch
        {
            CliColors.Yellow => "yellow",
            CliColors.Red => "red",
            CliColors.Lime => "lime",
            CliColors.Aqua => "aqua",
            CliColors.Blue => "deepskyblue1",
            CliColors.Green => "springgreen1",
            CliColors.InvertWhite => "black on white",
            CliColors.InvertYellow => "black on yellow",
            CliColors.InvertRed => "black on red",
            CliColors.InvertPurple => "black on fuchsia",
            CliColors.InvertAqua => "black on aqua",
            CliColors.InvertGreen => "black on springgreen1",
            _ => "white",
        };
    }
}