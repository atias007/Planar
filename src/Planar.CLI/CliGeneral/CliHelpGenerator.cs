using Microsoft.Extensions.Primitives;
using Planar.CLI.Actions;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Planar.CLI.CliGeneral
{
    internal static class CliHelpGenerator
    {
        private const string space = "  ";
        private const string cli = "planar-cli";
        private const string header0 = "<MODULE>";
        private const string header1 = "<COMMAND>";
        private const string header2 = "<ARGUMENTS>";

        public static void ShowModules()
        {
            const string header11 = "Module";
            const string header22 = "Description";

            AnsiConsole.Write(new FigletText("Planar")
                    .LeftJustified()
                    .Color(Color.SteelBlue));

            var panel = new Panel($" [steelblue]planar cli v{Program.Version}[/]")
            {
                Border = BoxBorder.Ascii
            };
            panel.BorderColor(Color.SteelBlue);

            AnsiConsole.Write(panel);
            AnsiConsole.WriteLine();
            var cliCommand = BaseCliAction.InteractiveMode ? string.Empty : $"{cli} ";
            AnsiConsole.MarkupLine($" [invert]usage:[/] {cliCommand}[lightskyblue1]{header0}[/] [cornsilk1]{header1}[/] [[{header2}]]");
            AnsiConsole.MarkupLine($" [invert]usage:[/] {cliCommand}[lightskyblue1]{header0}[/] [cornsilk1]--help'[/] to see all avalible commands and arguments");
            AnsiConsole.WriteLine();

            var grid = new Grid();
            grid.AddColumn();
            grid.AddColumn();
            grid.AddColumn();

            grid.AddRow(
                new Markup($"{space}"),
                new Markup($"[underline bold]{header11}[/]"),
                new Markup($"[underline bold]{header22}[/]"));

            var modules = BaseCliAction.GetModules();
            foreach (var m in modules)
            {
                grid.AddRow(
                    new Markup($"{space}"),
                    new Markup($"[cornsilk1]{m.Name}{space}[/]"),
                    new Markup($"{m.Description.EscapeMarkup()}"));
            }

            grid.AddEmptyRow();

            AnsiConsole.Write(grid);
        }

        public static void ShowHelp(string module, IEnumerable<CliActionMetadata> allActions)
        {
            var cliCommand = BaseCliAction.InteractiveMode ? string.Empty : $"{cli} ";

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($" [invert]usage:[/] {cliCommand}[lightskyblue1]{module}[/] [cornsilk1]{header1}[/] [[{header2}]]");
            AnsiConsole.WriteLine();

            var actions = GetActionsByModule(module, allActions);

            var grid = new Grid();
            grid.AddColumn();
            grid.AddColumn();
            grid.AddColumn();
            grid.AddRow(
                new Markup($"{space}"),
                new Markup($"[underline bold]{header1}[/]"),
                new Markup($"[underline bold]{header2}[/]")
            );

            foreach (var ac in actions)
            {
                const string wizardText = "[leave empty to open wizard...]";
                var mu = ac.HasWizard ?
                    new Markup($"{ac.ArgumentsDisplayName.EscapeMarkup()}\r\n[black on lightskyblue1]{wizardText.EscapeMarkup()}[/]") :
                    new Markup($"{ac.ArgumentsDisplayName.EscapeMarkup()}");

                grid.AddRow(
                    new Markup($"{space}"),
                    new Markup($"[cornsilk1]{ac.CommandDisplayName}{space}[/]"),
                    mu
                );
            }

            grid.AddEmptyRow();

            AnsiConsole.Write(grid);
        }

        public static string GetHelpMD(IEnumerable<CliActionMetadata> allActions)
        {
            var sb = new StringBuilder();
            var modules = BaseCliAction.GetModules();
            foreach (var item in modules)
            {
                var md = GetModuleMD(item.Name, allActions);
                sb.AppendLine(md);
            }

            return sb.ToString();
        }

        public static string GetModuleMD(string module, IEnumerable<CliActionMetadata> allActions)
        {
            var actions = GetActionsByModule(module, allActions);
            var sb = new StringBuilder();
            sb.AppendLine($"### {module}");
            sb.AppendLine("<table>");

            foreach (var ac in actions)
            {
                var command = HttpUtility.HtmlEncode(ac.CommandDisplayName);
                sb.AppendLine("\t<tr>");
                sb.AppendLine($"\t\t<td width=\"200px\"><code>{command}</code></td>");
                if (ac.ArgumentsDisplayNameItems.Any())
                {
                    var items = ac.ArgumentsDisplayNameItems.Select(i => $"<code>{HttpUtility.HtmlEncode(i)}</code>");
                    var itemsTitle = string.Join(' ', items);
                    sb.AppendLine($"\t\t<td>{itemsTitle}</td>");
                }
                else
                {
                    sb.AppendLine("\t\t<td></td>");
                }

                sb.AppendLine("\t</tr>");
            }

            sb.AppendLine("</table>");
            sb.AppendLine("<br/>");
            return sb.ToString();
        }

        private static IOrderedEnumerable<CliActionMetadata> GetActionsByModule(string module, IEnumerable<CliActionMetadata> allActions)
        {
            var actions = allActions
                .Where(a =>
                    string.Equals(a.Module, module, StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrEmpty(a.CommandDisplayName) &&
                    !a.IgnoreHelp)
                .OrderBy(a => a.CommandDisplayName);

            return actions;
        }
    }
}