using Planar.API.Common.Entities;
using Planar.CLI.CliGeneral;
using Spectre.Console;
using System;

namespace Planar.CLI
{
    public static class CliTableFormat
    {
        public static string FormatClusterHealthCheck(TimeSpan? span, TimeSpan? deviation)
        {
            var title = FromatDurationUpToSecond(span).EscapeMarkup();
            if (deviation.HasValue)
            {
                if (deviation.Value.TotalMilliseconds < 3000)
                {
                    return $"[{CliFormat.OkColor}]{title}[/]";
                }

                if (deviation.Value.TotalMilliseconds > 3000 && deviation.Value.TotalMilliseconds <= 6000)
                {
                    return $"[{CliFormat.WarningColor}]{title}[/]";
                }

                return $"[{CliFormat.ErrorColor}]{title}[/]";
            }

            return title;
        }

        public static string FormatDateTime(DateTime? dateTime)
        {
            if (dateTime.HasValue)
            {
                return FormatDateTime(dateTime.Value);
            }
            else
            {
                return string.Empty;
            }
        }

        public static string FormatDateTime(DateTime dateTime)
        {
            if (DateTime.Today == dateTime.Date)
            {
                return $"today      {dateTime.ToLongTimeString()}";
            }
            else
            {
                return $"{dateTime.ToShortDateString()} {dateTime.ToLongTimeString()}";
            }
        }

        public static string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalDays >= 1)
            {
                return $"{timeSpan:\\(d\\)\\ hh\\:mm\\:ss}";
            }

            return $"{timeSpan:hh\\:mm\\:ss}";
        }

        public static string FormatNumber(int number)
        {
            return $"{number:N0}";
        }

        public static string FormatNumber(int? number)
        {
            if (number.HasValue)
            {
                return FormatNumber(number.Value);
            }
            else
            {
                return string.Empty;
            }
        }

        public static string FromatDuration(int? number)
        {
            if (number.HasValue)
            {
                var value = number.Value;

                if (value < 1000)
                {
                    return $"{number} ms";
                }

                var span = TimeSpan.FromMilliseconds(number.Value);
                if (span.TotalSeconds < 60)
                {
                    return $"{span:ss\\.fff} sec";
                }

                if (span.TotalMinutes < 60)
                {
                    return $"{span:mm\\:ss} min";
                }

                if (span.TotalHours < 24)
                {
                    return $"{span:hh\\:mm\\:ss}";
                }

                return $"{span:\\(d\\)\\ hh\\:mm\\:ss}";
            }
            else
            {
                return string.Empty;
            }
        }

        public static string FromatDurationUpToSecond(TimeSpan? span)
        {
            if (span.HasValue)
            {
                var value = span.Value.TotalMilliseconds;

                if (value < 1000)
                {
                    return "now";
                }

                if (span.Value.TotalSeconds < 60)
                {
                    return $"{span:ss} sec";
                }

                if (span.Value.TotalMinutes < 60)
                {
                    return $"{span:mm\\:ss}";
                }

                if (span.Value.TotalHours < 24)
                {
                    return $"{span:hh\\:mm\\:ss}";
                }

                return $"{span:\\(d\\)\\ hh\\:mm\\:ss}";
            }
            else
            {
                return string.Empty;
            }
        }

        public static string GetStatusMarkup(int status)
        {
            var statusEnum = (StatusMembers)status;
            return statusEnum switch
            {
                StatusMembers.Running => $"[{CliFormat.WarningColor}]{statusEnum}[/]",
                StatusMembers.Success => $"[{CliFormat.OkColor}]{statusEnum}[/]",
                StatusMembers.Fail => $"[{CliFormat.ErrorColor}]{statusEnum}[/]",
                StatusMembers.Veto => $"[aqua]{statusEnum}[/]",
                _ => $"[silver]{statusEnum}[/]",
            };
        }

        public static string GetBooleanMarkup(bool value, object? display = null)
        {
            display ??= value;

            return value ?
                $"[{CliFormat.OkColor}]{display}[/]" :
                $"[{CliFormat.ErrorColor}]{display}[/]";
        }

        public static string GetLevelMarkup(string? level)
        {
            return level switch
            {
                "Warning" => $"[{CliFormat.WarningColor}]{level}[/]",
                "Information" => $"[{CliFormat.OkColor}]{level}[/]",
                "Error" => $"[{CliFormat.ErrorColor}]{level}[/]",
                "Fatal" => $"[{CliFormat.ErrorColor}]{level}[/]",
                "Debug" => $"[aqua]{level}[/]",
                "Verbose" => $"[silver]{level}[/]",
                _ => $"[silver]{level}[/]",
            };
        }

        public static string GetTriggerIdMarkup(string triggerId)
        {
            if (triggerId == Consts.ManualTriggerId)
            {
                return $"[invert]{triggerId.EscapeMarkup()}[/]";
            }
            else
            {
                return triggerId.EscapeMarkup();
            }
        }

        public static string GetProgressMarkup(int progress)
        {
            return $"[gold3_1]{progress}%[/]";
        }

        public static string GetFireInstanceIdMarkup(string fireInstanceId)
        {
            return $"[turquoise2]{fireInstanceId}[/]";
        }
    }
}