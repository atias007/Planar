using Planar.CLI.Actions;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Windows.Input;

namespace Planar.CLI.CliGeneral
{
    internal static class CliHelpGenerator
    {
        public const string CliCommand = "planar-cli";
        private const string space = "  ";
        private const string header0 = "<MODULE>";
        private const string header1 = "<COMMAND>";
        private const string header2 = "<ARGUMENTS>";

        // https://www.asciiart.eu/text-to-ascii-art
        // https://patorjk.com/software/taag/#p=display&f=Ivrit&t=planar%0APLANAR
        public static void ShowLogo2()
        {
            const string logo = """
                       _
                 _ __ | | __ _ _ __   __ _ _ __
                | '_ \| |/ _` | '_ \ / _` | '__|
                | |_) | | (_| | | | | (_| | |
                | .__/|_|\__,_|_| |_|\__,_|_|
                |_|

                """;
            AnsiConsole.Write(new Markup($"[steelblue1]{logo}[/]"));
        }

        public static void ShowLogo3()
        {
            // https://patorjk.com/software/taag/
            // https://coolors.co/palettes/trending
            Console.WriteLine();
            AnsiConsole.MarkupLine("[#023e8a]  ██████╗ ██╗      █████╗ ███╗   ██╗ █████╗ ██████╗ [/]");
            AnsiConsole.MarkupLine("[#0077b6]  ██╔══██╗██║     ██╔══██╗████╗  ██║██╔══██╗██╔══██╗[/]");
            AnsiConsole.MarkupLine("[#0096c7]  ██████╔╝██║     ███████║██╔██╗ ██║███████║██████╔╝[/]");
            AnsiConsole.MarkupLine("[#00b4d8]  ██╔═══╝ ██║     ██╔══██║██║╚██╗██║██╔══██║██╔══██╗[/]");
            AnsiConsole.MarkupLine("[#48cae4]  ██║     ███████╗██║  ██║██║ ╚████║██║  ██║██║  ██║[/]");
            AnsiConsole.MarkupLine($"[#90e0ef]  ╚═╝     ╚══════╝╚═╝  ╚═╝╚═╝  ╚═══╝╚═╝  ╚═╝╚═╝  ╚═╝  [[vesrion {Program.Version}]][/]");
        }

        public static void ShowLogo1()
        {
            AnsiConsole.Write(new FigletText("Planar")
                .LeftJustified()
                .Color(Color.SteelBlue));

            var panel = new Panel($" [steelblue]planar cli v{CliTableFormat.FormatVersion(Program.Version)}[/]")
            {
                Border = BoxBorder.Ascii
            };
            panel.BorderColor(Color.SteelBlue);

            AnsiConsole.Write(panel);
        }

        public static void ShowModules()
        {
            const string header11 = "Module";
            const string header22 = "Description";

            var cliCommand = BaseCliAction.InteractiveMode ? string.Empty : $"{CliCommand} ";
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

        public static void ShowInnerCommands()
        {
            var inner = BaseCliAction.GetInnerModule();
            ShowHelp(string.Empty, inner.Actions);
        }

        public static void ShowHelp(string module, IEnumerable<CliActionMetadata> allActions)
        {
            var cliCommand = BaseCliAction.InteractiveMode ? string.Empty : $"{CliCommand} ";
            var space = string.IsNullOrEmpty(cliCommand) ? string.Empty : " ";
            var prefix = $"{space}{cliCommand}";
            var lastSpace = string.IsNullOrEmpty(module) ? string.Empty : " ";
            if (string.IsNullOrEmpty(prefix)) { prefix = " "; }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($" [invert]usage:[/]{prefix}[lightskyblue1]{module}[/]{lastSpace}[cornsilk1]{header1}[/] [[{header2}]]");
            AnsiConsole.WriteLine();

            var actions =
                string.IsNullOrEmpty(module) ?
                allActions.OrderBy(a => a.CommandDisplayName) :
                GetActionsByModule(module, allActions);

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
                var mu = new Markup($"{ac.ArgumentsDisplayName.EscapeMarkup()}");

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
                if (ac.ArgumentsDisplayNameItems.Count != 0)
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