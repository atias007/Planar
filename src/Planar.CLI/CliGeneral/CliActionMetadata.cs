using Planar.CLI.Attributes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Planar.CLI
{
    public class CliActionMetadata
    {
        public string Module { get; set; } = string.Empty;

        public List<string> ModuleSynonyms { get; set; } = new();

        public List<string> Commands { get; set; } = new();

        public MethodInfo? Method { get; set; }

        public bool AllowNullRequest { get; set; }

        public List<CliArgumentMetadata> Arguments { get; set; } = new();

        public Type? RequestType { get; set; }

        public string CommandDisplayName { get; set; } = string.Empty;

        public string ArgumentsDisplayName { get; private set; } = string.Empty;

        public List<string> ArgumentsDisplayNameItems { get; private set; } = new List<string>();

        public bool IgnoreHelp { get; set; }

        public bool HasWizard { get; set; }

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
                else if (info != null && (info.PropertyType == typeof(DateTime) || info.PropertyType == typeof(DateTime?)))
                {
                    ArgumentsDisplayNameItems.Add($"[{item.DisplayName} <\"{GetCurrentDateTimeFormat()}>\"]");
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
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

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
}