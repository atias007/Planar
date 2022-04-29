using Planar.API.Common.Entities;
using Spectre.Console;
using System;

namespace Planar.CLI
{
    public static class CliTableFormat
    {
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
                return $"today      {dateTime:HH:mm:ss}";
            }
            else
            {
                return $"{dateTime:dd/MM/yyyy HH:mm:ss}";
            }
        }

        public static string FormatTimeSpan(TimeSpan timeSpan)
        {
            return $"{timeSpan:hh\\:mm\\:ss}";
        }

        public static string FromatNumber(int? number)
        {
            if (number.HasValue)
            {
                return FromatNumber(number.Value);
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
                    return $"{span:mm\\:ss}";
                }

                return $"{span:hh\\:mm\\:ss}";
            }
            else
            {
                return string.Empty;
            }
        }

        public static string FromatNumber(int number)
        {
            return $"{number:N0}";
        }

        public static string GetStatusMarkup(int status)
        {
            var statusEnum = (StatusMembers)status;
            return statusEnum switch
            {
                StatusMembers.Running => $"[khaki3]{statusEnum}[/]",
                StatusMembers.Success => $"[green]{statusEnum}[/]",
                StatusMembers.Fail => $"[red]{statusEnum}[/]",
                StatusMembers.Veto => $"[aqua]{statusEnum}[/]",
                _ => $"[silver]{statusEnum}[/]",
            };
        }

        public static string GetBooleanMarkup(bool value, object display = null)
        {
            if (display == null) { display = value; }

            return value ?
                $"[green]{display}[/]" :
                $"[red]{display}[/]";
        }

        public static string GetLevelMarkup(string level)
        {
            return level switch
            {
                "Warning" => $"[khaki3]{level}[/]",
                "Information" => $"[green]{level}[/]",
                "Error" => $"[red]{level}[/]",
                "Fatal" => $"[red]{level}[/]",
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