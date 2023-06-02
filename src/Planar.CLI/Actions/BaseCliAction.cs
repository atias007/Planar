using Planar.API.Common.Entities;
using Planar.CLI.Attributes;
using Planar.CLI.CliGeneral;
using Planar.CLI.Entities;
using Planar.CLI.General;
using Planar.CLI.Proxy;
using RestSharp;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.CLI.Actions
{
    public abstract class BaseCliAction
    {
        protected const string JobFileName = "JobFile.yml";

        public static bool InteractiveMode { get; set; }

        protected static void ValidateFileExists(string filename)
        {
            var fi = new FileInfo(filename);

            if (!fi.Exists)
            {
                throw new CliException($"file '{fi.FullName}' does not exists");
            }
        }

        protected static async Task<CliActionResponse> Execute(RestRequest request, CancellationToken cancellationToken = default)
        {
            var result = await RestProxy.Invoke(request, cancellationToken);
            return new CliActionResponse(result);
        }

        protected static string? CollectCliValue(string field, bool required, int minLength, int maxLength, string? regex = null, string? regexErrorMessage = null, string? defaultValue = null, bool secret = false)
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
            if (secret) { prompt.Secret(); }
            if (!string.IsNullOrEmpty(defaultValue))
            {
                prompt.DefaultValue(defaultValue);
            }

            var result = AnsiConsole.Prompt(prompt);

            return string.IsNullOrEmpty(result) ? null : result;
        }

        protected static async Task<CliActionResponse> ExecuteEntity<T>(RestRequest request, CancellationToken cancellationToken)
        {
            var result = await RestProxy.Invoke<T>(request, cancellationToken);
            if (result.IsSuccessful)
            {
                return new CliActionResponse(result, dumpObject: result.Data);
            }

            return new CliActionResponse(result);
        }

        protected static async Task<CliActionResponse> ExecuteTable<T>(RestRequest request, Func<T, CliTable> tableFunc, CancellationToken cancellationToken)
        {
            var result = await RestProxy.Invoke<T>(request, cancellationToken);
            if (result.IsSuccessful && result.Data != null)
            {
                var table = tableFunc.Invoke(result.Data);
                return new CliActionResponse(result, table);
            }

            return new CliActionResponse(result);
        }

        private static ValidationResult GetValidationResultError(string message)
        {
            var markup = CliFormat.GetErrorMarkup(message);
            return ValidationResult.Error(markup);
        }

        public static IEnumerable<CliActionMetadata> GetAllActions()
        {
            var modules = GetModules();
            var result = new List<CliActionMetadata>();
            result.AddRange(InnerCliActions.GetActions());

            foreach (var item in modules)
            {
                result.AddRange(item.Actions);
            }

            return result;
        }

        private static IEnumerable<CliModule>? _modules;

        public static IEnumerable<CliModule> GetModules()
        {
            if (_modules != null)
            {
                return _modules;
            }

            _modules = new List<CliModule>
            {
                GetModule<JobCliActions>(),
                GetModule<ServiceCliActions>(),
                GetModule<TriggerCliActions>(),
                GetModule<TraceCliActions>(),
                GetModule<ConfigCliActions>(),
                GetModule<HistoryCliActions>(),
                GetModule<UserCliActions>(),
                GetModule<GroupCliActions>(),
                GetModule<ClusterCliActions>(),
                GetModule<MonitorCliActions>()
            }
            .OrderBy(m => m.Name);

            return _modules;
        }

        private static CliModule GetModule<T>()
            where T : BaseCliAction<T>
        {
            var result = new CliModule();
            var type = typeof(T);

            // actions
            var method = type.GetMethod("GetActions",
                    BindingFlags.Static |
                    BindingFlags.FlattenHierarchy |
                    BindingFlags.InvokeMethod |
                    BindingFlags.Public);

            if (method?.Invoke(null, null) is IEnumerable<CliActionMetadata> actions)
            {
                result.Actions = actions;
            }

            // name and description
            var attribute = type.GetCustomAttribute<ModuleAttribute>();
            if (attribute != null)
            {
                result.Name = attribute.Name;
                result.Description = attribute.Description;
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
            // Console.WriteLine(id)
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
            return CliPromptUtil.PromptSelection(items, title, addCancelOption);
        }

        protected static bool ConfirmAction(string title)
        {
            if (!InteractiveMode) { return true; }
            return AnsiConsole.Confirm($"are you sure that you want to {title}?", false);
        }

        protected static int GetCounterHours()
        {
            var items = new[] { "1 hour", "2 hours", "8 hours", "1 day", "2 days", "3 days", "7 days" };
            var select = PromptSelection(items, "select time period", true);
            if (string.IsNullOrEmpty(select)) { return 0; }
            var parts = select.Split(' ');
            if (parts.Length != 2) { return 0; }
            if (!int.TryParse(parts[0], out var num)) { return 0; }
            if (parts[1].Length < 1) { return 0; }
            if (parts[1][0] == 'h') { return num; }
            if (parts[1][0] == 'd') { return num * 24; }
            return 0;
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

            // TODO: check for moduleAttribute == null
            foreach (var act in allActions)
            {
                var actionAttributes = act.GetCustomAttributes<ActionAttribute>();
                var nullRequestAttribute = act.GetCustomAttribute<NullRequestAttribute>();
                var ignoreHelpAttribute = act.GetCustomAttribute<IgnoreHelpAttribute>();
                var hasWizard = act.GetCustomAttribute<ActionWizardAttribute>();

                // TODO: validate attributes (invalid name...)
                if (actionAttributes == null || !actionAttributes.Any()) { continue; }

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
                    IgnoreHelp = ignoreHelpAttribute != null,
                    HasWizard = hasWizard != null
                };

                if (!string.IsNullOrEmpty(moduleAttribute?.Synonyms))
                {
                    item.ModuleSynonyms = moduleAttribute.Synonyms.Split(',').ToList();
                }

                if (!string.IsNullOrEmpty(item.Module))
                {
                    item.ModuleSynonyms.Add(item.Module);
                }

                item.SetArgumentsDisplayName();

                result.Add(item);
            }

            return result;
        }

        private static Type? GetRequestType(MethodInfo? method)
        {
            if (method == null) { return null; }

            var parameters = method.GetParameters();
            if (parameters.Length == 0)
            {
                throw new CliException($"cli error: action '{method.Name}' has no parameters");
            }

            if (parameters.Length > 2)
            {
                throw new CliException($"cli error: action '{method.Name}' has more then 2 parameter");
            }

            var last = parameters.Last();
            if (last.ParameterType != typeof(CancellationToken))
            {
                throw new CliException($"cli error: action '{method.Name}' has no CancellationToken parameter");
            }

            var requestType =
                parameters.Length == 1 ?
                null :
                parameters.First().ParameterType;

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
    }
}