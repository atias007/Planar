using Spectre.Console;
using System.Linq;
using System.Text;

namespace Planar.CLI.CliGeneral
{
    internal static partial class CliFormat
    {
        internal const string WarningColor = "khaki3";
        internal const string OkColor = "green";
        internal const string ErrorColor = "Red";
        //internal const string Suggestion = "turquoise2";

        internal static readonly string Seperator = string.Empty.PadLeft(50, '-');

        public static string GetWarningMarkup(string? message)
        {
            message ??= string.Empty;
            return $"[black on {WarningColor}]warning:[/] [{WarningColor}]{message.EscapeMarkup()}[/]";
        }

        public static string GetErrorMarkup(string? message)
        {
            message ??= string.Empty;
            return $"[black on {ErrorColor}]error:[/] [{ErrorColor}]{message.EscapeMarkup()}[/]";
        }

        public static string GetSuggestionMarkup(string? message)
        {
            if (string.IsNullOrWhiteSpace(message)) { return string.Empty; }
            var sb = new StringBuilder();
            //sb.Append($"[{Suggestion}]");
            sb.AppendLine(Seperator);
            sb.AppendLine("suggestion:");
            sb.AppendLine();
            sb.AppendLine(message.EscapeMarkup().Trim());
            sb.Append(Seperator);
            //sb.AppendLine("[/]");
            return sb.ToString();
        }

        public static string GetValidationErrorMarkup(string? message)
        {
            message ??= string.Empty;
            return $"[black on {ErrorColor}]validation error:[/] [{ErrorColor}]{message.EscapeMarkup()}[/]";
        }

        public static string GetUnauthorizedErrorMarkup()
        {
            return $"[black on {ErrorColor}]unauthorized:[/] [{ErrorColor}]you must login to perform this action[/]";
        }

        public static string GetForbiddenErrorMarkup()
        {
            return $"[black on {ErrorColor}]forbidden:[/] [{ErrorColor}]you don't have the permission to perform this action[/]";
        }

        public static string GetConflictErrorMarkup(string? message)
        {
            message ??= string.Empty;
            return $"[black on {ErrorColor}]conflict error:[/] [{ErrorColor}]{message.EscapeMarkup()}[/]";
        }

        public static string? GetLogMarkup(string? log)
        {
            if (string.IsNullOrWhiteSpace(log)) { return null; }
            var sb = new StringBuilder();
            var lines = log.Split('\n').Select(l => l?.Trim() ?? string.Empty);
            string? lastColor = null;
            foreach (var item in lines)
            {
                var matches = _regex.Matches(item);
                if (matches.Count > 0)
                {
                    var level = matches[0].Groups[1].Value;
                    lastColor = GetColor(level);
                    sb.AppendLine($"[{lastColor}]{item.EscapeMarkup()}[/]");
                }
                else
                {
                    if (lastColor != null)
                    {
                        sb.AppendLine($"[{lastColor}]{item.EscapeMarkup()}[/]");
                    }
                    else
                    {
                        sb.AppendLine(item.EscapeMarkup());
                    }
                }
            }

            return sb.ToString();
        }
    }
}