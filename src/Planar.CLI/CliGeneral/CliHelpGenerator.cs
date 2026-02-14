using Planar.CLI.Actions;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Planar.CLI.CliGeneral;

internal static class CliHelpGenerator
{
    public const string CliCommand = "planar-cli";
    private const string space = "  ";
    private const string header0 = "<MODULE>";
    private const string header1 = "<COMMAND>";
    private const string header2 = "<ARGUMENTS>";

    // https://www.asciiart.eu/text-to-ascii-art
    // https://patorjk.com/software/taag/#p=display&f=Ivrit&t=planar%0APLANAR

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
        AnsiConsole.MarkupLine($"[#90e0ef]  ╚═╝     ╚══════╝╚═╝  ╚═╝╚═╝  ╚═══╝╚═╝  ╚═╝╚═╝  ╚═╝[/]");
        AnsiConsole.MarkupLine($"  [#FF875F][[vesrion {Program.Version}]][/]");
    }

    public static void ShowModules()
    {
        // usage panel
        var cliCommand = BaseCliAction.InteractiveMode ? string.Empty : $"{CliCommand} ";
        var line1 = $"{cliCommand}[#0077b6]{header0}[/] [khaki1]{header1}[/] [[{header2}]]";
        var line2 = $"{cliCommand}[#0077b6]{header0}[/] --help    (to see all avalible commands and arguments)";
        var panel1 = new Panel($"{line1}\r\n{line2}")
        {
            Header = new PanelHeader($"[invert] usage [/]"),
            Border = BoxBorder.Rounded
        };

        AnsiConsole.Write(panel1);

        // commands panel
        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();

        var modules = BaseCliAction.GetModules();
        foreach (var m in modules)
        {
            grid.AddRow(
                new Markup($"[#0077b6]{m.Name}{space}[/]"),
                new Markup($"{m.Description.EscapeMarkup()}"));
        }

        var panel2 = new Panel(grid)
        {
            Header = new PanelHeader($"[invert] modules [/]"),
            Border = BoxBorder.Rounded
        };
        AnsiConsole.Write(panel2);
    }

    public static void ShowInnerCommands()
    {
        var inner = BaseCliAction.GetInnerModule();
        ShowHelp(string.Empty, inner.Actions);
    }

    public static void ShowHelp(string module, IEnumerable<CliActionMetadata> allActions)
    {
        AnsiConsole.WriteLine();

        var cliCommand = BaseCliAction.InteractiveMode ? string.Empty : $"{CliCommand} ";
        var spaceChar = string.IsNullOrEmpty(cliCommand) ? string.Empty : " ";
        var prefix = $"{spaceChar}{cliCommand}";
        var lastSpace = string.IsNullOrEmpty(module) ? string.Empty : " ";
        if (string.IsNullOrEmpty(prefix)) { prefix = " "; }

        // usage panel
        var line1 = $"{prefix}{module}{lastSpace}[khaki1]{header1}[/] [[{header2}]]";
        var panel1 = new Panel(line1)
        {
            Header = new PanelHeader($"[invert] usage [/]"),
            Border = BoxBorder.Rounded
        };
        AnsiConsole.Write(panel1);

        // commands panel
        var actions =
            string.IsNullOrEmpty(module) ?
            allActions.OrderBy(a => a.CommandDisplayName) :
            GetActionsByModule(module, allActions);

        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();
        grid.AddRow(
            new Markup($"[underline bold]{header1}[/]"),
            new Markup($"[underline bold]{header2}[/]")
        );

        foreach (var ac in actions)
        {
            var mu = new Markup($"{ac.ArgumentsDisplayName.EscapeMarkup()}");
            grid.AddRow(
                new Markup($"[khaki1]{ac.CommandDisplayName}{spaceChar}[/]"),
                mu
            );
        }

        var panel2 = new Panel(grid)
        {
            Header = new PanelHeader($"[invert] commands & arguments [/]"),
            Border = BoxBorder.Rounded
        };
        AnsiConsole.Write(panel2);
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