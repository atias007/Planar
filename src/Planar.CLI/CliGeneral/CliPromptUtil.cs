using Planar.API.Common.Entities;
using Planar.CLI.General;
using Planar.CLI.Proxy;
using RestSharp;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

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

        internal static async Task<CliPromptWrapper<string>> Groups(CancellationToken cancellationToken)
        {
            var restRequest = new RestRequest("group", Method.Get)
                .AddQueryPagingParameter(1000);

            var result = await RestProxy.Invoke<PagingResponse<GroupInfo>>(restRequest, cancellationToken);
            if (!result.IsSuccessful)
            {
                return new CliPromptWrapper<string>(result);
            }

            var data = result.Data?.Data;
            if (data == null || !data.Any())
            {
                throw new CliWarningException("no available groups to perform the opertaion");
            }

            var items = data.Select(g => g.Name ?? string.Empty);
            var select = PromptSelection(items, "group", true);
            return new CliPromptWrapper<string>(select);
        }

        internal static async Task<CliPromptWrapper<string>> GroupsForUser(string username, CancellationToken cancellationToken)
        {
            var restRequest = new RestRequest("user/{username}", Method.Get)
               .AddParameter("username", username, ParameterType.UrlSegment);
            var userResult = await RestProxy.Invoke<UserDetails>(restRequest, cancellationToken);
            if (!userResult.IsSuccessful)
            {
                return new CliPromptWrapper<string>(userResult);
            }

            var items = userResult.Data?.Groups;
            if (items == null || !items.Any())
            {
                throw new CliWarningException("no available groups to perform the opertaion");
            }

            var select = PromptSelection(items, "group", true);
            return new CliPromptWrapper<string>(select);
        }

        internal static async Task<CliPromptWrapper<string>> GroupsWithoutUser(string username, CancellationToken cancellationToken)
        {
            var restRequest = new RestRequest("group", Method.Get)
                .AddQueryPagingParameter(1000);

            var result = await RestProxy.Invoke<PagingResponse<GroupInfo>>(restRequest, cancellationToken);
            if (!result.IsSuccessful)
            {
                return new CliPromptWrapper<string>(result);
            }

            var data = result.Data?.Data;
            if (data == null || !data.Any())
            {
                throw new CliWarningException("no available groups to perform the opertaion");
            }

            restRequest = new RestRequest("user/{username}", Method.Get)
               .AddParameter("username", username, ParameterType.UrlSegment);
            var userResult = await RestProxy.Invoke<UserDetails>(restRequest, cancellationToken);
            if (!userResult.IsSuccessful)
            {
                return new CliPromptWrapper<string>(userResult);
            }

            var groupsincludeUser = userResult.Data?.Groups;
            var items =
                groupsincludeUser == null ?
                data.Select(g => g.Name ?? string.Empty) :
                data.Select(g => g.Name ?? string.Empty).Except(groupsincludeUser);

            if (!items.Any())
            {
                throw new CliWarningException("no available groups to perform the opertaion");
            }

            var select = PromptSelection(items, "group", true);
            return new CliPromptWrapper<string>(select);
        }

        internal static async Task<CliPromptWrapper<string>> Users(CancellationToken cancellationToken)
        {
            var restRequest = new RestRequest("user", Method.Get)
                .AddQueryPagingParameter(1000);
            var result = await RestProxy.Invoke<PagingResponse<UserRowModel>>(restRequest, cancellationToken);
            if (!result.IsSuccessful)
            {
                return new CliPromptWrapper<string>(result);
            }

            var data = result.Data?.Data;
            if (data == null || !data.Any())
            {
                throw new CliWarningException("no available users to perform the opertaion");
            }

            var items = data.Select(g => g.Username ?? string.Empty);
            var select = PromptSelection(items, "user", true);
            return new CliPromptWrapper<string>(select);
        }

        internal static async Task<CliPromptWrapper<string>> UsersInGroup(string groupName, CancellationToken cancellationToken)
        {
            var restRequest = new RestRequest("group/{name}", Method.Get)
                .AddParameter("name", groupName, ParameterType.UrlSegment);

            var result = await RestProxy.Invoke<GroupDetails>(restRequest, cancellationToken);
            if (!result.IsSuccessful)
            {
                return new CliPromptWrapper<string>(result);
            }

            var items = result.Data?.Users.Select(u => u.Username);
            if (items == null || !items.Any())
            {
                throw new CliWarningException("no available users to perform the opertaion");
            }

            var select = PromptSelection(items, "user", true);
            return new CliPromptWrapper<string>(select);
        }

        internal static async Task<CliPromptWrapper<string>> UsersNotInGroup(string groupName, CancellationToken cancellationToken)
        {
            var restRequest = new RestRequest("user", Method.Get)
                .AddQueryPagingParameter(1000);
            var result = await RestProxy.Invoke<PagingResponse<UserRowModel>>(restRequest, cancellationToken);
            if (!result.IsSuccessful)
            {
                return new CliPromptWrapper<string>(result);
            }

            var data = result.Data?.Data;
            if (data == null || !data.Any())
            {
                throw new CliWarningException("no available users to perform the opertaion");
            }

            restRequest = new RestRequest("group/{name}", Method.Get)
                .AddParameter("name", groupName, ParameterType.UrlSegment);

            var groupResult = await RestProxy.Invoke<GroupDetails>(restRequest, cancellationToken);
            if (!groupResult.IsSuccessful)
            {
                return new CliPromptWrapper<string>(groupResult);
            }

            var usersInGroup = groupResult.Data?.Users.Select(u => u.Username);
            var items =
                usersInGroup == null ?
                data.Select(g => g.Username ?? string.Empty) :
                data.Select(g => g.Username ?? string.Empty).Except(usersInGroup);

            if (!items.Any())
            {
                throw new CliWarningException("no available users to perform the opertaion");
            }

            var select = PromptSelection(items, "user", true);
            return new CliPromptWrapper<string>(select);
        }

        internal static async Task<CliPromptWrapper<string>> Reports(CancellationToken cancellationToken)
        {
            var restRequest = new RestRequest("report", Method.Get);
            var result = await RestProxy.Invoke<List<string>>(restRequest, cancellationToken);
            if (!result.IsSuccessful)
            {
                return new CliPromptWrapper<string>(result);
            }

            var data = result.Data;
            if (data == null || !data.Any())
            {
                throw new CliWarningException("no available reports to perform the opertaion");
            }

            var select = PromptSelection(data, "report", true);
            return new CliPromptWrapper<string>(select);
        }

        internal static async Task<CliPromptWrapper<string>> ReportPeriods(CancellationToken cancellationToken)
        {
            var restRequest = new RestRequest("report/periods", Method.Get);
            var result = await RestProxy.Invoke<List<string>>(restRequest, cancellationToken);
            if (!result.IsSuccessful)
            {
                return new CliPromptWrapper<string>(result);
            }

            var data = result.Data;
            if (data == null || !data.Any())
            {
                throw new CliWarningException("no available periods to perform the opertaion");
            }

            var select = PromptSelection(data, "period", true);
            return new CliPromptWrapper<string>(select);
        }

        internal static async Task<CliPromptWrapper<Roles>> Roles(CancellationToken cancellationToken)
        {
            var restRequest = new RestRequest("group/roles", Method.Get);
            var result = await RestProxy.Invoke<List<string>>(restRequest, cancellationToken);
            if (!result.IsSuccessful)
            {
                return new CliPromptWrapper<Roles>(result);
            }

            if (result.Data == null || !result.Data.Any())
            {
                throw new CliWarningException("no available roles to perform the opertaion");
            }

            var items = result.Data.Select(g => g ?? string.Empty);
            var select = PromptSelection(items, "role", true);

            if (!Enum.TryParse<Roles>(select, ignoreCase: true, out var enumSelect))
            {
                throw new CliException($"fail to convert selected value '{select}' to valid planar role");
            }

            return new CliPromptWrapper<Roles>(enumSelect);
        }

        internal static void CheckForCancelOption(string? value)
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

        internal static DateTime? PromptForDate(string title)
        {
            var select = AnsiConsole.Prompt(
                new TextPrompt<string>($"[turquoise2]  > {title.EscapeMarkup()} [grey]({CliActionMetadata.GetCurrentDateTimeFormat()}):[/][/]")
                .AllowEmpty()
                .Validate(date =>
                {
                    if (string.IsNullOrWhiteSpace(date))
                    {
                        return ValidationResult.Success();
                    }

                    date = date.Trim();
                    if (!DateTime.TryParse(date, CultureInfo.CurrentCulture, out var _))
                    {
                        return ValidationResult.Error("[red]invalid date format[/]");
                    }

                    return ValidationResult.Success();
                }));

            if (string.IsNullOrEmpty(select))
            {
                return null;
            }

            return DateTime.Parse(select, CultureInfo.CurrentCulture);
        }
    }
}