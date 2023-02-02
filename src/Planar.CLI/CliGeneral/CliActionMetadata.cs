using Planar.CLI.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Planar.CLI
{
    public class CliActionMetadata
    {
        public string Module { get; set; } = string.Empty;

        public List<string> Commands { get; set; } = new();

        public MethodInfo? Method { get; set; }

        public bool AllowNullRequest { get; set; }

        public List<CliArgumentMetadata> Arguments { get; set; } = new();

        public Type? RequestType { get; set; }

        public string CommandDisplayName { get; set; } = string.Empty;

        public string ArgumentsDisplayName { get; private set; } = string.Empty;

        public bool IgnoreHelp { get; set; }

        public void SetArgumentsDisplayName()
        {
            var sb = new StringBuilder();
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
                    var options = GetEnumOptions(enumType);
                    if (!string.IsNullOrEmpty(options))
                    {
                        title = options;
                    }
                }

                if (item.Required)
                {
                    sb.Append($"<{title}> ");
                }
                else
                {
                    sb.Append($"[<{title}>] ");
                }
            }

            foreach (var item in otherArgs)
            {
                var enumType = item.EnumType;
                var info = item.PropertyInfo;

                if (info != null && info.PropertyType == typeof(bool))
                {
                    sb.Append($"[{item.DisplayName}] ");
                }
                else if (enumType != null)
                {
                    var options = GetEnumOptions(enumType);
                    sb.Append($"[{item.DisplayName} <{options}>] ");
                }
                else
                {
                    sb.Append($"[{item.DisplayName} <value>] ");
                }
            }

            ArgumentsDisplayName = sb.ToString().Trim();
        }

        private static string GetEnumOptions(Type type)
        {
            var parts = new List<string>();
            var items = type.GetMembers(BindingFlags.Public | BindingFlags.Static);
            foreach (var item in items)
            {
                if (item == null) { continue; }
                var att = item.GetCustomAttribute<ActionEnumOptionAttribute>();
                if (att == null)
                {
                    parts.Add(item.Name);
                }
                else
                {
                    parts.Add(att.Name);
                }
            }

            return string.Join('|', parts);
        }
    }
}