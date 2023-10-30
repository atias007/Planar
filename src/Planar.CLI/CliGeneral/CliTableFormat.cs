using Planar.API.Common.Entities;
using Planar.CLI.CliGeneral;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Text;

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

        public static string FormatDate(DateTime? dateTime)
        {
            if (dateTime.HasValue)
            {
                return FormatDate(dateTime.Value);
            }
            else
            {
                return string.Empty;
            }
        }

        public static string FormatDate(DateTime dateTime)
        {
            if (DateTime.Today == dateTime.Date)
            {
                return $"today     ";
            }
            else
            {
                return $"{dateTime.ToShortDateString()}";
            }
        }

        public static string FormatTime(DateTime? dateTime)
        {
            if (dateTime.HasValue)
            {
                return FormatTime(dateTime.Value);
            }
            else
            {
                return string.Empty;
            }
        }

        public static string FormatTime(DateTime dateTime)
        {
            return $"{dateTime.ToShortTimeString()}";
        }

        public static string FormatExceptionCount(int count)
        {
            if (count <= 0) { return "0"; }
            return $"[red]{count}[/]";
        }

        public static string FormatJobKey(string? group, string? name)
        {
            var noGroup = string.IsNullOrWhiteSpace(group);
            var noName = string.IsNullOrWhiteSpace(name);

            if (noName && noGroup) { return string.Empty; }
            if (noName) { return group!.EscapeMarkup(); }
            if (noGroup) { return $"DEFAULT.{name}".EscapeMarkup(); }
            return $"{group}.{name}".EscapeMarkup();
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

        public static string FormatSummaryNumber(int number, string? color = null)
        {
            if (number == 0) { return $"[gray]-[/]"; }
            if (string.IsNullOrEmpty(color)) { return $"{number:N0}"; }
            return $"[{color}]{number:N0}[/]";
        }

        public static string FormatTimeSpan(TimeSpan? timeSpan)
        {
            if (timeSpan == null) { return "--:--:--"; }
            return FormatTimeSpan(timeSpan.Value);
        }

        public static string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalDays >= 1)
            {
                return $"{timeSpan:\\(d\\)\\ hh\\:mm\\:ss}";
            }

            return $"{timeSpan:hh\\:mm\\:ss}";
        }

        public static string FormatTimeSpanShort(TimeSpan timeSpan)
        {
            return $"{timeSpan:h\\:mm}";
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

        public static string GetBooleanMarkup(bool value, object? display = null)
        {
            display ??= value;

            return value ?
                $"[{CliFormat.OkColor}]{display}[/]" :
                $"[{CliFormat.ErrorColor}]{display}[/]";
        }

        public static string GetBooleanWarningMarkup(bool value, object? display = null)
        {
            display ??= value;

            return value ?
                $"[{CliFormat.WarningColor}]{display}[/]" :
                $"{display}";
        }

        public static string GetFireInstanceIdMarkup(string fireInstanceId)
        {
            return $"[turquoise2]{fireInstanceId}[/]";
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

        public static string GetProgressMarkup(int progress)
        {
            return $"[gold3_1]{progress}%[/]";
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

        public static string GetTimeScopeString(IEnumerable<WorkingHourScopeModel> scopes)
        {
            var sb = new StringBuilder();
            foreach (var scope in scopes)
            {
                sb.Append(FormatTimeSpanShort(scope.Start));
                sb.Append(" - ");
                sb.AppendLine(FormatTimeSpanShort(scope.End));
            }
            return sb.ToString().Trim();
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
    }
}