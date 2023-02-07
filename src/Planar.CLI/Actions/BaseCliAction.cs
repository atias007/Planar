﻿using Newtonsoft.Json.Linq;
using Planar.API.Common.Entities;
using Planar.CLI.Attributes;
using Planar.CLI.CliGeneral;
using Planar.CLI.Entities;
using Planar.CLI.General;
using RestSharp;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Planar.CLI.Actions
{
    public abstract class BaseCliAction
    {
        protected const string CancelOption = "<cancel>";
        protected const string JobFileName = "JobFile.yml";

        public static bool InteractiveMode { get; set; }

        protected static async Task<CliActionResponse> Execute(RestRequest request)
        {
            var result = await RestProxy.Invoke(request);
            return new CliActionResponse(result);
        }

        protected static string? CollectCliValue(string field, bool required, int minLength, int maxLength, string? regex = null, string? regexErrorMessage = null)
        {
            var prompt = new TextPrompt<string>($"[turquoise2]  > {field.EscapeMarkup()}: [/]")
                .Validate(value =>
                {
                    if (required && string.IsNullOrWhiteSpace(value)) { return GetValidationResultError($"{field} is required field"); }
                    value = value.Trim();

                    if (string.IsNullOrEmpty(value) && !required)
                    {
                        return ValidationResult.Success();
                    }

                    if (value.Length > maxLength) { return GetValidationResultError($"{field} limited to {maxLength} chars maximum"); }
                    if (value.Length < minLength) { return GetValidationResultError($"{field} must have at least {minLength} chars"); }
                    if (!string.IsNullOrEmpty(regex))
                    {
                        var rx = new Regex(regex, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
                        if (!rx.IsMatch(value))
                        {
                            return GetValidationResultError($"{field} {regexErrorMessage}");
                        }
                    }

                    return ValidationResult.Success();
                });

            if (!required) { prompt.AllowEmpty(); }

            var result = AnsiConsole.Prompt(prompt);

            return string.IsNullOrEmpty(result) ? null : result;
        }

        protected static async Task<CliActionResponse> ExecuteEntity<T>(RestRequest request)
        {
            var result = await RestProxy.Invoke<T>(request);
            if (result.IsSuccessful)
            {
                return new CliActionResponse(result, serializeObj: result.Data);
            }

            return new CliActionResponse(result);
        }

        protected static async Task<CliActionResponse> ExecuteTable<T>(RestRequest request, Func<T, Table> tableFunc)
        {
            var result = await RestProxy.Invoke<T>(request);
            if (result.IsSuccessful && result.Data != null)
            {
                var table = tableFunc.Invoke(result.Data);
                return new CliActionResponse(result, table);
            }

            return new CliActionResponse(result);
        }

        private static object? ConvertJTokenToObject(JToken token)
        {
            if (token is JValue value) { return value.Value; }

            if (token is JArray)
            {
                return token.AsEnumerable().Select(ConvertJTokenToObject).ToList();
            }

            if (token is JObject)
            {
                return token.AsEnumerable().Cast<JProperty>().ToDictionary(x => x.Name, x => ConvertJTokenToObject(x.Value));
            }

            throw new InvalidOperationException("unexpected token: " + token);
        }

        private static ValidationResult GetValidationResultError(string message)
        {
            var markup = CliFormat.GetErrorMarkup(message);
            return ValidationResult.Error(markup);
        }

        public static IEnumerable<CliActionMetadata> GetAllActions()
        {
            var result = new List<CliActionMetadata>();
            result.AddRange(InnerCliActions.GetActions());
            result.AddRange(JobCliActions.GetActions());
            result.AddRange(ServiceCliActions.GetActions());
            result.AddRange(TriggerCliActions.GetActions());
            result.AddRange(TraceCliActions.GetActions());
            result.AddRange(ConfigCliActions.GetActions());
            result.AddRange(HistoryCliActions.GetActions());
            result.AddRange(UserCliActions.GetActions());
            result.AddRange(GroupCliActions.GetActions());
            result.AddRange(ClusterCliActions.GetActions());
            result.AddRange(MonitorCliActions.GetActions());
            return result;
        }
    }

    public class BaseCliAction<T> : BaseCliAction
    {
        public static IEnumerable<CliActionMetadata> GetActions()
        {
            var result = new List<CliActionMetadata>();
            var type = typeof(T);
            var allActions = type.GetMethods(BindingFlags.Public | BindingFlags.Static).ToList();
            var moduleAttribute = type.GetCustomAttribute<ModuleAttribute>();

            // TODO: check for moduleAttribute == null;
            foreach (var act in allActions)
            {
                var actionAttributes = act.GetCustomAttributes<ActionAttribute>();
                var nullRequestAttribute = act.GetCustomAttribute<NullRequestAttribute>();
                var ignoreHelpAttribute = act.GetCustomAttribute<IgnoreHelpAttribute>();

                // TODO: validate attributes (invalid name...)
                if (actionAttributes == null) { continue; }
                var requestType = GetRequestType(act);
                var comnmands = actionAttributes.Select(a => a.Name).Distinct().ToList();
                var item = new CliActionMetadata
                {
                    Module = moduleAttribute?.Name?.ToLower() ?? string.Empty,
                    Method = act,
                    Commands = comnmands,
                    AllowNullRequest = nullRequestAttribute != null,
                    RequestType = requestType,
                    Arguments = GetArguments(requestType),
                    CommandDisplayName = string.Join('|', comnmands.OrderBy(c => c.Length)),
                    IgnoreHelp = ignoreHelpAttribute != null
                };

                item.SetArgumentsDisplayName();

                result.Add(item);
            }

            return result;
        }

        private static Type? GetRequestType(MethodInfo? method)
        {
            if (method == null) { return null; }

            var parameters = method.GetParameters();
            if (parameters.Length == 0) { return null; }
            if (parameters.Length > 1)
            {
                throw new CliException($"cli error: action '{method.Name}' has more then 1 parameter");
            }

            var requestType = parameters[0].ParameterType;
            return requestType;
        }

        private static List<CliArgumentMetadata> GetArguments(Type? requestType)
        {
            var result = new List<CliArgumentMetadata>();
            if (requestType == null)
            {
                return result;
            }

            var props = requestType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var isJobKey =
                requestType.IsAssignableFrom(typeof(CliJobKey)) ||
                requestType.IsSubclassOf(typeof(CliJobKey));

            var isTriggerKey =
                requestType.IsAssignableFrom(typeof(CliTriggerKey)) ||
                requestType.IsSubclassOf(typeof(CliTriggerKey));

            foreach (var item in props)
            {
                var att = item.GetCustomAttribute<ActionPropertyAttribute>();
                var req = item.GetCustomAttribute<RequiredAttribute>();
                var info = new CliArgumentMetadata
                {
                    PropertyInfo = item,
                    LongName = att?.LongName?.ToLower(),
                    ShortName = att?.ShortName?.ToLower(),
                    DisplayName = att?.DisplayName,
                    Default = (att?.Default).GetValueOrDefault(),
                    Required = req != null,
                    RequiredMissingMessage = req?.Message,
                    DefaultOrder = (att?.DefaultOrder).GetValueOrDefault(),
                    JobKey = isJobKey && item.Name == nameof(CliJobKey.Id),
                    TriggerKey = isTriggerKey && item.Name == nameof(CliTriggerKey.Id),
                };
                result.Add(info);
            }

            return result;
        }

        protected static void AssertCreated(RestResponse<JobIdResponse> response)
        {
            if (!response.IsSuccessful) { return; }
            Util.SetLastJobOrTriggerId(response);
            Console.WriteLine(response?.Data?.Id);
        }

        protected static void AssertJobUpdated(RestResponse<JobIdResponse> response)
        {
            if (!response.IsSuccessful) { return; }
            Util.SetLastJobOrTriggerId(response);
            AssertUpdated(response?.Data?.Id, "job");
        }

        protected static void AssertJobDataUpdated(RestResponse response, string id)
        {
            if (!response.IsSuccessful) { return; }
            AssertUpdated(id, "job");
        }

        protected static void AssertTriggerUpdated(RestResponse response, string id)
        {
            if (!response.IsSuccessful) { return; }
            AssertUpdated(id, "trigger");
        }

        protected static void AssertUpdated(string? id, string entity)
        {
            if (string.IsNullOrEmpty(id)) { return; }
            Console.WriteLine(id);
            string message = entity switch
            {
                "job" => CliFormat.GetWarningMarkup("job is in 'pause' state and none of its triggers will fire"),
                "trigger" => CliFormat.GetWarningMarkup("trigger is in 'pause' state and it will not fire"),
                _ => string.Empty,
            };

            if (!string.IsNullOrEmpty(message))
            {
                AnsiConsole.MarkupLine(message);
            }
        }

        protected static string? PromptSelection(IEnumerable<string>? items, string title, bool addCancelOption = true)
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

            var selectedItem = AnsiConsole.Prompt(
                 new SelectionPrompt<string>()
                     .Title($"[underline][gray]select [/][white]{title?.EscapeMarkup()}[/][gray] from the following list (press [/][blue]enter[/][gray] to select):[/][/]")
                     .PageSize(20)
                     .MoreChoicesText($"[grey](Move [/][blue]up[/][grey] and [/][blue]down[/] [grey]to reveal more [/][white]{title?.EscapeMarkup()}s[/])")
                     .AddChoices(finalItems));

            CheckForCancelOption(selectedItem);

            return selectedItem;
        }

        protected static void CheckForCancelOption(string value)
        {
            if (value == CancelOption)
            {
                throw new CliWarningException("operation was canceled");
            }
        }

        protected static void CheckForCancelOption(IEnumerable<string> values)
        {
            if (values.Any(v => v == CancelOption))
            {
                throw new CliWarningException("operation was canceled");
            }
        }

        protected static bool ConfirmAction(string title)
        {
            if (!InteractiveMode) { return true; }
            return AnsiConsole.Confirm($"are you sure that you want to {title}?", false);
        }
    }
}