using Planar.CLI.Actions;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Planar.CLI.CliGeneral
{
    internal static class CliHelpGenerator
    {
        public static void ShowHelp(string module, IEnumerable<CliActionMetadata> allActions)
        {
            const string cli = "planar-cli";
            const string header1 = "<COMMAND>";
            const string header2 = "<ARGUMENTS>";
            const string space = "   ";

            var cliCommand = BaseCliAction.InteractiveMode ? string.Empty : $"{cli} ";

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($" [invert]usage:[/] {cliCommand}[lightskyblue1]{module}[/] [cornsilk1]{header1}[/] [[{header2}]]");
            AnsiConsole.WriteLine();

            var actions = allActions
                .Where(a =>
                    string.Equals(a.Module, module, StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrEmpty(a.CommandDisplayName) &&
                    !a.IgnoreHelp)
                .OrderBy(a => a.CommandDisplayName);

            var grid = new Grid();
            grid.AddColumn();
            grid.AddColumn();
            grid.AddColumn();
            grid.AddRow(new Markup[] {
                new Markup($"{space}"),
                new Markup($"[cornsilk1 underline bold]{header1}[/]"),
                new Markup($"[underline bold]{header2}[/]")
            });

            foreach (var ac in actions)
            {
                const string wizardText = "[leave empty to open wizard...]";
                var mu = ac.HasWizard ?
                    new Markup($"{ac.ArgumentsDisplayName.EscapeMarkup()}\r\n[black on lightskyblue1]{wizardText.EscapeMarkup()}[/]") :
                    new Markup($"{ac.ArgumentsDisplayName.EscapeMarkup()}");

                grid.AddRow(new Markup[] {
                    new Markup($"{space}"),
                    new Markup($"[cornsilk1]{ac.CommandDisplayName}[/]"),
                    mu
                });
            }

            grid.AddEmptyRow();

            AnsiConsole.Write(grid);
        }
    }
}