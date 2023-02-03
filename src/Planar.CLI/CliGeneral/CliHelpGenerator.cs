using Spectre.Console;
using Spectre.Console.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Planar.CLI.CliGeneral
{
    internal static class CliHelpGenerator
    {
        public static void ShowHelp(string module, IEnumerable<CliActionMetadata> allActions)
        {
            const string header1 = "<command>";
            const string header2 = "<arguments>";

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($" [invert]usage:[/] planar-cli [lightskyblue1]{module}[/] {header1}[[{header2}]]");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($" [underline]the options for {header1} and [[{header2}]] parameters of [lightskyblue1]{module}[/] module are:[/]");
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
            grid.AddRow(new Markup[] {
                new Markup($" [grey54 underline]{header1}[/]"),
                new Markup($"[grey54 underline]{header2}[/]")
            });

            foreach (var ac in actions)
            {
                grid.AddRow(new Markup[] {
                    new Markup($" {ac.CommandDisplayName}"),
                    new Markup($" {ac.ArgumentsDisplayName.EscapeMarkup()}")
                });
            }

            grid.AddEmptyRow();

            AnsiConsole.Write(grid);
        }
    }
}