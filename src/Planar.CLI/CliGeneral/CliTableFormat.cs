using Planar.API.Common.Entities;
using Planar.CLI.CliGeneral;
using Planar.CLI.General;
using Planar.Common;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Text;

namespace Planar.CLI;

public static class CliTableFormat
{
    public const char bullet = '•';

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
            return $"{dateTime:d}";
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
        return $"{dateTime:t}";
    }

    public static string FormatExceptionCount(int count)
    {
        if (count <= 0) { return "-"; }
        return $"[red]{count:N0}[/]";
    }

    public static string FormatJobId(string jobId, JobActiveMembers active)
    {
        if (active == JobActiveMembers.Active) { return $"  {jobId}"; }
        if (active == JobActiveMembers.PartiallyActive) { return $"[{CliFormat.WarningColor}]{bullet}[/] {jobId}"; }
        if (active == JobActiveMembers.NoTrigger) { return $"[{CliFormat.Suggestion}]{bullet}[/] {jobId}"; }
        if (active == JobActiveMembers.Inactive) { return $"[{CliFormat.ErrorColor}]{bullet}[/] {jobId}"; }

        return jobId;
    }

    public static string FormatTriggerId(string id, bool active)
    {
        if (active) { return $"  {id.EscapeMarkup()}"; }
        return $"[{CliFormat.ErrorColor}]{bullet}[/] {id}";
    }

    public static string FormatJobKey(string? group, string? name)
    {
        var noGroup = string.IsNullOrWhiteSpace(group);
        var noName = string.IsNullOrWhiteSpace(name);

        if (noName && noGroup) { return string.Empty; }
        if (noName) { return $"{CliConsts.GroupDisplayFormat}{group!.EscapeMarkup()}[/]"; }
        if (noGroup) { return $"{CliConsts.GroupDisplayFormat}default.[/]{name.EscapeMarkup()}"; }
        return $"{CliConsts.GroupDisplayFormat}{group.EscapeMarkup()}.[/]{name.EscapeMarkup()}";
    }

    public static string UnformatJobName(string formattedName)
    {
        if (string.IsNullOrWhiteSpace(formattedName)) { return string.Empty; }
        return formattedName.Replace($"{CliConsts.GroupDisplayFormat}", string.Empty).Replace("[/]", string.Empty);
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

    public static string GetStatusMarkup(int status, bool hasWarning)
    {
        var result = GetStatusMarkup(status);
        if (hasWarning)
        {
            result = $"{result} [{CliFormat.WarningColor}]{bullet}[/]";
        }

        return result;
    }

    public static string GetStatusMarkup(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return GetStatusMarkup(-1);
        }

        if (Enum.TryParse<StatusMembers>(title, out var status))
        {
            return GetStatusMarkup((int)status);
        }

        return GetStatusMarkup(-1);
    }

    public static string GetStatusMarkup(int status)
    {
        var statusEnum = (StatusMembers)status;
        return statusEnum switch
        {
            StatusMembers.Running => $"[{CliFormat.WarningColor}]{statusEnum}[/]",
            StatusMembers.Success => $"[{CliFormat.OkColor}]{statusEnum}[/]",
            StatusMembers.Fail => $"[{CliFormat.ErrorColor}]{statusEnum}[/]",
            ////StatusMembers.Veto => $"[aqua]{statusEnum}[/]",
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

    public static string GetTriggerIdMarkup(string? triggerId)
    {
        if (string.IsNullOrWhiteSpace(triggerId))
        {
            return string.Empty;
        }

        if (triggerId == Consts.ManualTriggerId)
        {
            return $"[invert]{triggerId.EscapeMarkup()}[/]";
        }
        else if (triggerId == Consts.SequenceTriggerId)
        {
            return $"[black on lightskyblue3_1]{triggerId.EscapeMarkup()}[/]";
        }
        else
        {
            return triggerId.EscapeMarkup();
        }
    }

    public static string FormatActive(JobActiveMembers active)
    {
        var text = active.ToString().SplitWords();
        return active switch
        {
            JobActiveMembers.Active => $"[{CliFormat.OkColor}]{text}[/]",
            JobActiveMembers.PartiallyActive => $"[{CliFormat.WarningColor}]{text}[/]",
            JobActiveMembers.NoTrigger => $"[{CliFormat.Suggestion}]{text}[/]",
            JobActiveMembers.Inactive => $"[{CliFormat.ErrorColor}]{text}[/]",
            _ => text,
        };
    }

    public static string FormatVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return string.Empty;
        }

        var parts = version.Split('.');
        if (parts.Length > 3)
        {
            return $"{parts[0]}.{parts[1]}.{parts[2]}{CliConsts.GroupDisplayFormat}.{parts[3]}[/]";
        }

        return version;
    }
}