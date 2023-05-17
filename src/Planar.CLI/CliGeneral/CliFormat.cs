using Spectre.Console;

namespace Planar.CLI.CliGeneral
{
    internal static class CliFormat
    {
        internal const string WarningColor = "khaki3";
        internal const string OkColor = "green";
        internal const string ErrorColor = "Red";

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
    }
}