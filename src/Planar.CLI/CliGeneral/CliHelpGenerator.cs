using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Planar.CLI.CliGeneral
{
    internal static class CliHelpGenerator
    {
        public static void ShowHelp(string module, IEnumerable<CliActionMetadata> allActions)
        {
            var rule = new Rule();
            rule.RuleStyle(new Style(foreground: new Color(138, 138, 138)));

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($" [invert]usage:[/] planar-cli [lightskyblue1]{module}[/] <command> [[<arguments>]]");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($" the options for <command> and [[<options>]] parameters of [lightskyblue1]{module}[/] module are:");
            AnsiConsole.WriteLine();

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

            AnsiConsole.MarkupLine($" {"[grey54]<command>[/]"}{string.Empty.PadLeft(totalLength - 9, ' ')}[grey54]<arguments>[/]");
            AnsiConsole.Write(rule);

            foreach (var ac in actions)
            {
                AnsiConsole.Write(' ');
                AnsiConsole.Write(ac.CommandDisplayName.PadRight(totalLength));
                AnsiConsole.MarkupLine(ac.ArgumentsDisplayName.EscapeMarkup());
            }

            AnsiConsole.WriteLine();
        }
    }
}