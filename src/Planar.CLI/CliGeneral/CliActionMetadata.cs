using Planar.CLI.Attributes;
using Planar.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Planar.CLI;

public class CliActionMetadata
{
    public const string TimeSpan = "timespan";

    public string Module { get; set; } = string.Empty;

    public List<string> ModuleSynonyms { get; set; } = [];

    public List<string> Commands { get; set; } = [];

    public MethodInfo? Method { get; set; }

    public bool AllowNullRequest { get; set; }

    public List<CliArgumentMetadata> Arguments { get; set; } = [];

    public Type? RequestType { get; set; }

    public string CommandDisplayName { get; set; } = string.Empty;

    public string ArgumentsDisplayName { get; private set; } = string.Empty;

    public List<string> ArgumentsDisplayNameItems { get; private set; } = [];

    public bool IgnoreHelp { get; set; }

    public string CommandsTitle => string.Join('|', Commands);

    public void SetArgumentsDisplayName()
    {
        var defaultArgs = Arguments
            .Where(a => a.Default)
            .OrderBy(a => a.DefaultOrder);

        var otherArgs = Arguments
            .Where(a => !a.Default)
            .OrderBy(a => a.DisplayName);

        foreach (var item in defaultArgs)
        {
            var title = string.IsNullOrEmpty(item.DisplayName) ? item.Name?.ToLower() : item.DisplayName.ToLower();
            var enumType = item.EnumType;

            if (enumType != null)
            {
                var options = GetEnumOptionsTitle(enumType);
                if (!string.IsNullOrEmpty(options))
                {
                    title = options;
                }
            }

            if (item.Required)
            {
                ArgumentsDisplayNameItems.Add($"<{title}>");
            }
            else
            {
                ArgumentsDisplayNameItems.Add($"[<{title}>]");
            }
        }

        foreach (var item in otherArgs)
        {
            var enumType = item.EnumType;
            var info = item.PropertyInfo;

            if (info != null && info.PropertyType == typeof(bool))
            {
                ArgumentsDisplayNameItems.Add($"[{item.DisplayName}]");
            }
            else if (info != null && info.PropertyType.Is<DateTime>())
            {
                ArgumentsDisplayNameItems.Add($"[{item.DisplayName} <date time>]");
            }
            else if (info != null && info.PropertyType.Is<int>())
            {
                ArgumentsDisplayNameItems.Add($"[{item.DisplayName} <number>]");
            }
            else if (info != null && info.PropertyType.Is<bool>())
            {
                ArgumentsDisplayNameItems.Add($"[{item.DisplayName} <true|false>]");
            }
            else if (info != null && info.PropertyType.Is<TimeSpan>())
            {
                ArgumentsDisplayNameItems.Add($"[{item.DisplayName} <{TimeSpan}>]");
            }
            else if (info != null && info.PropertyType == typeof(Dictionary<string, string>))
            {
                ArgumentsDisplayNameItems.Add($"[{item.DisplayName} <key=value>]");
            }
            else if (enumType != null)
            {
                var options = GetEnumOptionsTitle(enumType);
                ArgumentsDisplayNameItems.Add($"[{item.DisplayName} <{options}>]");
            }
            else
            {
                ArgumentsDisplayNameItems.Add($"[{item.DisplayName} <value>]");
            }
        }

        ArgumentsDisplayName = string.Join(' ', ArgumentsDisplayNameItems);
    }

    public static string GetCurrentDateTimeFormat()
    {
        var ci = CultureInfo.CurrentCulture;
        string shortDateFormatString = ci.DateTimeFormat.ShortDatePattern;
        string shortTimeFormatString = ci.DateTimeFormat.LongTimePattern;
        return $"{shortDateFormatString} {shortTimeFormatString}";
    }

    public static IEnumerable<string> GetEnumOptions(Type type)
    {
        var parts = new List<string>();
        var items = type.GetMembers(BindingFlags.Public | BindingFlags.Static);
        foreach (var item in items)
        {
            if (item == null) { continue; }
            var att = item.GetCustomAttribute<ActionEnumOptionAttribute>();
            if (att == null)
            {
                parts.Add(ToMinusCase(item.Name));
            }
            else
            {
                parts.Add(att.Name);
            }
        }

        return parts;
    }

    private static string GetEnumOptionsTitle(Type type)
    {
        var parts = GetEnumOptions(type);
        return string.Join('|', parts);
    }

    public static string ToMinusCase(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (text.Length < 2)
        {
            return text;
        }

        var sb = new StringBuilder();
        sb.Append(char.ToLowerInvariant(text[0]));
        for (int i = 1; i < text.Length; ++i)
        {
            char c = text[i];
            if (char.IsUpper(c))
            {
                sb.Append('-');
                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }
}