using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Planar.CLI.CliGeneral
{
    internal static class CliHelpGenerator
    {
        public static string GetHelpMarkup(string module, IEnumerable<CliActionMetadata> allActions)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"usage: planar-cli {module} <command> [<options>]");
            sb.AppendLine();
            sb.AppendLine($"the options for <command> and [<options>] parameters of {module} module:");
            sb.AppendLine();

            var actions = allActions
                .Where(a =>
                    string.Equals(a.Module, module, StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrEmpty(a.CommandDisplayName) &&
                    !a.IgnoreHelp)
                .OrderBy(a => a.CommandDisplayName);

            var maxCommandLength = actions
                .Where(a => a != null)
                .Select(a => a.CommandDisplayName?.Length).Max();
            var totalLength = maxCommandLength.GetValueOrDefault() + 4;

            sb.AppendLine($" {"<command>".PadRight(totalLength)}<arguments>");
            sb.AppendLine(string.Empty.PadLeft(100, '-'));

            foreach (var ac in actions)
            {
                sb.Append(' ');
                sb.Append(ac.CommandDisplayName?.PadRight(totalLength));
                sb.AppendLine(ac.ArgumentsDisplayName);
            }

            sb.AppendLine(string.Empty.PadLeft(100, '-'));

            return sb.ToString().Trim();
        }
    }
}