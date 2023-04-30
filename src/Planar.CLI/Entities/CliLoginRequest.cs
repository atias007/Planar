using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliLoginRequest
    {
        [ActionProperty(DefaultOrder = 1)]
        public string Host { get; set; } = string.Empty;

        [ActionProperty(DefaultOrder = 2)]
        public int Port { get; set; }

        [ActionProperty(LongName = "secure", ShortName = "s")]
        public bool SecureProtocol { get; set; }

        [ActionProperty(LongName = "remember", ShortName = "r")]
        public bool Remember { get; set; }

        [ActionProperty(LongName = "remember-days", ShortName = "rd")]
        public int RememberDays { get; set; }

        [ActionProperty(LongName = "username", ShortName = "u")]
        public string? Username { get; set; }

        [ActionProperty(LongName = "password", ShortName = "p")]
        public string? Password { get; set; }

        [ActionProperty("c", "color")]
        public CliColors Color { get; set; }

        [IterativeActionProperty]
        public bool Iterative { get; set; }

        internal string Key => $"{Host}:{Port}";

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
}