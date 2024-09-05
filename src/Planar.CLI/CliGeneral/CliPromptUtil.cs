using Planar.API.Common.Entities;
using Planar.CLI.General;
using Planar.CLI.Proxy;
using RestSharp;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.CLI.CliGeneral;

internal static class CliPromptUtil
{
    internal static string? PromptSelection(IEnumerable<string>? items, string title)
    {
        if (items == null) { return null; }

        var finalItems = items.Select(i => new CliSelectItem<string> { DisplayName = i, Value = i });
        return PromptSelection(finalItems, title)?.Value;
    }

    internal static CliSelectItem<T>? PromptSelection<T>(IEnumerable<CliSelectItem<T>>? items, string title)
    {
        if (items == null) { return null; }
        var finalItems = items.ToList();
        var addSearch = finalItems.Count > 5;
        finalItems.Add(CliSelectItem<T>.CancelItem);

        using var _ = new TokenBlockerScope();
        var prompt = new SelectionPrompt<CliSelectItem<T>>()
                 .Title($"[underline][gray]select [/][white]{title?.EscapeMarkup()}[/][gray] from the following list (press [/][blue]enter[/][gray] to select):[/][/]")
                 .PageSize(20)
                 .MoreChoicesText($"[grey](Move [/][blue]up[/][grey] and [/][blue]down[/] [grey]to reveal more [/][white]{title?.EscapeMarkup()}s[/])")
                 .AddChoices(finalItems);
        if (addSearch)
        {
            prompt.EnableSearch();
            prompt.SearchHighlightStyle = new Style(foreground: Color.White, background: Color.DeepSkyBlue4_2);
        }
        var selectedItem = AnsiConsole.Prompt(prompt);

        CheckForCancelOption(selectedItem);

        return selectedItem;
    }

    internal static async Task<CliPromptWrapper<string>> NewHooks(CancellationToken cancellationToken)
    {
        var restRequest = new RestRequest("monitor/new-hooks", Method.Get);
        var result = await RestProxy.Invoke<IEnumerable<string>>(restRequest, cancellationToken);
        if (!result.IsSuccessful)
        {
            return new CliPromptWrapper<string>(result);
        }

        var data = result.Data;
        if (data == null || !data.Any())
        {
            throw new CliWarningException("no available new hooks to perform the opertaion");
        }

        var items = data.Select(g => g ?? string.Empty);
        var select = PromptSelection(items, "new hook");
        return new CliPromptWrapper<string>(select);
    }

    internal static async Task<CliPromptWrapper<string>> ExternalHooks(CancellationToken cancellationToken)
    {
        var restRequest = new RestRequest("monitor/hooks", Method.Get);
        var result = await RestProxy.Invoke<IEnumerable<HookInfo>>(restRequest, cancellationToken);
        if (!result.IsSuccessful)
        {
            return new CliPromptWrapper<string>(result);
        }

        var data = result.Data;
        if (data == null || !data.Any())
        {
            throw new CliWarningException("no available new hooks to perform the opertaion");
        }

        var items = data
            .Where(h => string.Equals(h.HookType, "external", StringComparison.OrdinalIgnoreCase))
            .Select(g => g.Name ?? string.Empty);

        var select = PromptSelection(items, "hook");
        return new CliPromptWrapper<string>(select);
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
        if (data == null || data.Count == 0)
        {
            throw new CliWarningException("no available groups to perform the opertaion");
        }

        var items = data.Select(g => g.Name ?? string.Empty);
        var select = PromptSelection(items, "group");
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
        if (items == null || items.Count == 0)
        {
            throw new CliWarningException("no available groups to perform the opertaion");
        }

        var select = PromptSelection(items, "group");
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
        if (data == null || data.Count == 0)
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

        var select = PromptSelection(items, "group");
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
        if (data == null || data.Count == 0)
        {
            throw new CliWarningException("no available users to perform the opertaion");
        }

        var items = data.Select(g => g.Username ?? string.Empty);
        var select = PromptSelection(items, "user");
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

        var select = PromptSelection(items, "user");
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
        if (data == null || data.Count == 0)
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

        var select = PromptSelection(items, "user");
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
        if (data == null || data.Count == 0)
        {
            throw new CliWarningException("no available reports to perform the opertaion");
        }

        var select = PromptSelection(data, "report");
        return new CliPromptWrapper<string>(select);
    }

    internal static async Task<CliPromptWrapper<MonitorItem>> Monitors(CancellationToken cancellationToken)
    {
        var restRequest = new RestRequest("monitor", Method.Get)
            .AddQueryPagingParameter(1000);
        var result = await RestProxy.Invoke<PagingResponse<MonitorItem>>(restRequest, cancellationToken);
        if (!result.IsSuccessful)
        {
            return new CliPromptWrapper<MonitorItem>(result);
        }

        var data = result.Data?.Data;
        if (data == null || data.Count == 0)
        {
            throw new CliWarningException("no available monitors to perform the opertaion");
        }

        var prompt = data.Select(data => $"{data.Id} - {data.Title}");
        var select = PromptSelection(prompt, "monitor");
        var id = select?.Split('-').FirstOrDefault()?.Trim();
        _ = int.TryParse(id, out var monitorId);
        var item = data.First(data => data.Id == monitorId);
        return new CliPromptWrapper<MonitorItem>(item);
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
        if (data == null || data.Count == 0)
        {
            throw new CliWarningException("no available periods to perform the opertaion");
        }

        var select = PromptSelection(data, "period");
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

        if (result.Data == null || result.Data.Count == 0)
        {
            throw new CliWarningException("no available roles to perform the opertaion");
        }

        var items = result.Data.Select(g => g ?? string.Empty);
        var select = PromptSelection(items, "role");

        if (!Enum.TryParse<Roles>(select, ignoreCase: true, out var enumSelect))
        {
            throw new CliException($"fail to convert selected value '{select}' to valid planar role");
        }

        return new CliPromptWrapper<Roles>(enumSelect);
    }

    internal static void CheckForCancelOption(CliSelectItem? value)
    {
        if (value == null) { return; }
        if (value.IsCancelItem)
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

    internal static TimeSpan? PromptForTimeSpan(string title, bool required = false)
    {
        var select = AnsiConsole.Prompt(
            new TextPrompt<string>($"[turquoise2]  > {title.EscapeMarkup()} [grey]([[days.]]hh:mm:ss):[/][/]")
            .AllowEmpty()
            .Validate(ts =>
            {
                if (required && string.IsNullOrWhiteSpace(ts))
                {
                    return ValidationResult.Error("[red]value is required[/]");
                }

                var span = ParseTimeSpan(ts);
                if (span == null)
                {
                    return ValidationResult.Error("[red]invalid time span format[/]");
                }

                return ValidationResult.Success();
            }));

        if (!required && string.IsNullOrEmpty(select)) { return null; }

        return ParseTimeSpan(select);
    }

    private static TimeSpan? ParseTimeSpan(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) { return null; }

        value = value.Trim();
        var parseTs = ParseTimeSpanPharse(value);
        if (parseTs != null) { return parseTs; }

        if (TimeSpan.TryParse(value, CultureInfo.CurrentCulture, out var newTs))
        {
            return newTs;
        }

        return null;
    }

    private static TimeSpan? ParseTimeSpanPharse(string value)
    {
        var regex = new Regex("^(\\d+)(\\s)?(sec|second|seconds|min|minute|minutes|hour|hours|day|days)$", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
        var match = regex.Matches(value);
        if (match.Count == 0) { return null; }
        if (match[0].Groups.Count < 4) { return null; }

        var number = int.Parse(match[0].Groups[1].Value);
        var unit = match[0].Groups[3].Value;

        return unit switch
        {
            "sec" or "second" or "seconds" => (TimeSpan?)TimeSpan.FromSeconds(number),
            "min" or "minute" or "minutes" => (TimeSpan?)TimeSpan.FromMinutes(number),
            "hour" or "hours" => (TimeSpan?)TimeSpan.FromHours(number),
            "day" or "days" => (TimeSpan?)TimeSpan.FromDays(number),
            _ => null,
        };
    }
}