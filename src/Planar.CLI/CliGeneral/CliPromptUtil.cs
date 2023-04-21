using Planar.API.Common.Entities;
using Planar.CLI.Entities;
using Planar.CLI.General;
using Planar.CLI.Proxy;
using RestSharp;
using Spectre.Console;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.CLI.CliGeneral
{
    internal static class CliPromptUtil
    {
        internal const string CancelOption = "<cancel>";

        internal static string? PromptSelection(IEnumerable<string>? items, string title, bool addCancelOption = true)
        {
            if (items == null) { return null; }
            IEnumerable<string> finalItems;
            if (addCancelOption)
            {
                var temp = items.ToList();
                temp.Add(CancelOption);
                finalItems = temp;
            }
            else
            {
                finalItems = items;
            }

            using var _ = new TokenBlockerScope();
            var selectedItem = AnsiConsole.Prompt(
                 new SelectionPrompt<string>()
                     .Title($"[underline][gray]select [/][white]{title?.EscapeMarkup()}[/][gray] from the following list (press [/][blue]enter[/][gray] to select):[/][/]")
                     .PageSize(20)
                     .MoreChoicesText($"[grey](Move [/][blue]up[/][grey] and [/][blue]down[/] [grey]to reveal more [/][white]{title?.EscapeMarkup()}s[/])")
                     .AddChoices(finalItems));

            CheckForCancelOption(selectedItem);

            return selectedItem;
        }

        internal static async Task<CliPromptWrapper<int>> Groups(CancellationToken cancellationToken)
        {
            var restRequest = new RestRequest("group", Method.Get);
            var result = await RestProxy.Invoke<List<GroupInfo>>(restRequest, cancellationToken);
            if (!result.IsSuccessful)
            {
                return new CliPromptWrapper<int>(result);
            }

            if (result.Data == null || !result.Data.Any())
            {
                throw new CliWarningException("no available groups to perform the opertaion");
            }

            var items = result.Data.Select(g => g.Name ?? string.Empty);
            var select = PromptSelection(items, "group", true);
            var id = result.Data.Where(r => r.Name == select).Select(r => r.Id).FirstOrDefault();
            return new CliPromptWrapper<int>(id);
        }

        internal static async Task<CliPromptWrapper<int>> Users(CancellationToken cancellationToken)
        {
            var restRequest = new RestRequest("user", Method.Get);
            var result = await RestProxy.Invoke<List<UserRowDetails>>(restRequest, cancellationToken);
            if (!result.IsSuccessful)
            {
                return new CliPromptWrapper<int>(result);
            }

            if (result.Data == null || !result.Data.Any())
            {
                throw new CliWarningException("no available users to perform the opertaion");
            }

            var items = result.Data.Select(g => g.Username ?? string.Empty);
            var select = PromptSelection(items, "user", true);
            var id = result.Data.Where(r => r.Username == select).Select(r => r.Id).FirstOrDefault();
            return new CliPromptWrapper<int>(id);
        }

        private static void CheckForCancelOption(string value)
        {
            if (value == CancelOption)
            {
                throw new CliWarningException("operation was canceled");
            }
        }

        internal static void CheckForCancelOption(IEnumerable<string> values)
        {
            if (values.Any(v => v == CancelOption))
            {
                throw new CliWarningException("operation was canceled");
            }
        }
    }
}