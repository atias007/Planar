﻿using Microsoft.AspNetCore.DataProtection;
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
using System.Globalization;
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

        protected static void FillMissingDataProperties(ICliDataRequest request)
        {
            if (request.Action == null)
            {
                var items = Enum.GetNames<DataActions>()
                    .Select(n => n.ToLower())
                    .OrderBy(n => n);

                var action = CliPromptUtil.PromptSelection(items, "action") ?? string.Empty;
                request.Action = Enum.Parse<DataActions>(action, true);
            }

            if (request.Action == DataActions.Clear) { return; }

            if (string.IsNullOrWhiteSpace(request.DataKey))
            {
                request.DataKey = CollectCliValue(
                    field: "key",
                    required: true,
                    minLength: 1,
                    maxLength: 100) ?? string.Empty;
            }

            if (request.Action == DataActions.Remove) { return; }

            if (string.IsNullOrWhiteSpace(request.DataValue) && request.Action == DataActions.Put)
            {
                request.DataValue = CollectCliValue(
                    field: "value",
                    required: false,
                    minLength: 0,
                    maxLength: 1000);
            }
        }

        protected static void FillOptionalString<T>(T entity, string propertyName, string? defaultValue = null, bool secret = false)
            where T : class
        {
            var tuple = CollectText(entity, propertyName, false, -1, defaultValue, secret);
            if (tuple.Item1)
            {
                tuple.Item3.SetValue(entity, tuple.Item2);
            }
        }

        protected static void FillRequiredString<T>(T entity, string propertyName, string? defaultValue = null, bool secret = false)
            where T : class
        {
            var tuple = CollectText(entity, propertyName, true, 1, defaultValue, secret);
            if (tuple.Item1)
            {
                tuple.Item3.SetValue(entity, tuple.Item2);
            }
        }

        protected static void FillRequiredInt<T>(T entity, string propertyName, string? defaultValue = null, bool secret = false)
            where T : class
        {
            var tuple = CollectText(entity, propertyName, true, 1, defaultValue, secret,
                v =>
                {
                    if (!int.TryParse(v, out _))
                    {
                        return GetValidationResultError("invalid number");
                    }

                    return ValidationResult.Success();
                }
            );

            if (tuple.Item1 && int.TryParse(tuple.Item2, out var intValue))
            {
                tuple.Item3.SetValue(entity, intValue);
            }
        }

        protected static void FillRequiredLong<T>(T entity, string propertyName, string? defaultValue = null, bool secret = false)
            where T : class
        {
            var tuple = CollectText(entity, propertyName, true, 1, defaultValue, secret,
                v =>
                {
                    if (!long.TryParse(v, out _))
                    {
                        return GetValidationResultError("invalid number");
                    }

                    return ValidationResult.Success();
                }
            );

            if (tuple.Item1 && long.TryParse(tuple.Item2, out var intValue))
            {
                tuple.Item3.SetValue(entity, intValue);
            }
        }

        protected static void FillRequiredTimeSpan<T>(T entity, string propertyName, string? defaultValue = null, bool secret = false)
            where T : class
        {
            var tuple = CollectText(entity, propertyName, true, 1, defaultValue, secret,
                v =>
                {
                    if (!TimeSpan.TryParse(v, CultureInfo.CurrentCulture, out _))
                    {
                        return GetValidationResultError("invalid time span value");
                    }

                    return ValidationResult.Success();
                }
            );

            if (tuple.Item1 && TimeSpan.TryParse(tuple.Item2, CultureInfo.CurrentCulture, out var tsValue))
            {
                tuple.Item3.SetValue(entity, tsValue);
            }
        }

        private static (bool, string?, PropertyInfo) CollectText<T>(T entity, string propertyName, bool required, int minLength, string? defaultValue, bool secret, Func<string, ValidationResult>? validation = null)
        where T : class
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
            var info = ReflectionHelper.GetPropertyInfo<T>(propertyName);
            var value = info.GetValue(entity)?.ToString();
            var attribute = ReflectionHelper.GetActionPropertyAttribute<T>(propertyName);
            var displayName = attribute.DisplayName ?? string.Empty;
            if (string.IsNullOrWhiteSpace(displayName)) { displayName = propertyName; }

            if (IsEmpty(value, info.PropertyType)) { value = string.Empty; }

            if (string.IsNullOrWhiteSpace(value))
            {
                value = CollectCliValue(
                     field: displayName.ToLower(),
                     required: required,
                     minLength: minLength,
                     maxLength: int.MaxValue,
                     defaultValue: defaultValue,
                     secret: secret,
                     validation: validation) ?? string.Empty;

                return (true, value, info);
            }

            return (false, null, info);
        }

        private static bool IsEmpty(string? value, Type type)
        {
            const string zero = "0";

            var isNumeric = type.IsPrimitive &&
                 (type == typeof(int) || type == typeof(double) ||
                  type == typeof(float) || type == typeof(byte) ||
                  type == typeof(sbyte) || type == typeof(short) ||
                  type == typeof(ushort) || type == typeof(long) ||
                  type == typeof(ulong));

            if (isNumeric)
            {
                return value == zero;
            }

            if (type == typeof(TimeSpan) && TimeSpan.TryParse(value, CultureInfo.CurrentCulture, out var ts))
            {
                return ts == TimeSpan.Zero;
            }

            return string.IsNullOrWhiteSpace(value);
        }

        protected static int? CollectNumericCliValue(string field, bool required, int minValue, int maxValue, int? defaultValue = null)
        {
            var prompt = new TextPrompt<string>($"[turquoise2]  > {field.EscapeMarkup()?.Trim()}:[/]")
                .Validate(value =>
                {
                    if (required && string.IsNullOrWhiteSpace(value)) { return GetValidationResultError($"{field} is required field"); }
                    value = value.Trim();

                    if (string.IsNullOrEmpty(value) && !required)
                    {
                        return ValidationResult.Success();
                    }

                    if (!int.TryParse(value, out var num))
                    {
                        return GetValidationResultError($"{field} must be a valid integer number");
                    }

                    if (num > maxValue) { return GetValidationResultError($"{field} limited to maximum value of {maxValue}"); }
                    if (num < minValue) { return GetValidationResultError($"{field} limited to minimum value of {minValue}"); }

                    return ValidationResult.Success();
                });

            if (!required) { prompt.AllowEmpty(); }
            if (defaultValue.HasValue)
            {
                prompt.DefaultValue(defaultValue.GetValueOrDefault().ToString(CultureInfo.CurrentCulture));
            }

            var result = AnsiConsole.Prompt(prompt);
            return int.Parse(result);
        }

        protected static string? CollectCliValue(string field, bool required, int minLength, int? maxLength, string? regex = null, string? regexErrorMessage = null, string? defaultValue = null, bool secret = false, Func<string, ValidationResult>? validation = null)
        {
            var prompt = new TextPrompt<string>($"[turquoise2]  > {field.EscapeMarkup()?.Trim()}:[/]")
                .Validate(value =>
                {
                    if (required && string.IsNullOrWhiteSpace(value)) { return GetValidationResultError($"{field} is required field"); }
                    value = value.Trim();

                    if (string.IsNullOrEmpty(value) && !required)
                    {
                        return ValidationResult.Success();
                    }

                    if (maxLength.HasValue && value.Length > maxLength) { return GetValidationResultError($"{field} limited to {maxLength} chars maximum"); }
                    if (value.Length < minLength) { return GetValidationResultError($"{field} must have at least {minLength} chars"); }
                    if (!string.IsNullOrEmpty(regex))
                    {
                        var rx = new Regex(regex, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
                        if (!rx.IsMatch(value))
                        {
                            return GetValidationResultError($"{field} {regexErrorMessage}");
                        }
                    }

                    if (validation != null)
                    {
                        var customValidation = validation.Invoke(value);
                        if (!customValidation.Successful)
                        {
                            return customValidation;
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
            where T : class
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
        private static CliModule? _innerModule;

        public static CliModule GetInnerModule()
        {
            if (_innerModule != null)
            {
                return _innerModule;
            }

            _innerModule = GetModule<InnerCliActions>();
            return _innerModule;
        }

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
                GetModule<MonitorCliActions>(),
                GetModule<MetricsCliActions>(),
                GetModule<ReportCliActions>()
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

        protected static void AssertCreated(RestResponse<PlanarIdResponse> response)
        {
            if (!response.IsSuccessful) { return; }
            Util.SetLastJobOrTriggerId(response);
            Console.WriteLine(response.Data?.Id);
        }

        protected static void AssertJobUpdated(RestResponse<PlanarIdResponse> response)
        {
            if (!response.IsSuccessful) { return; }
            Util.SetLastJobOrTriggerId(response);
            AssertUpdated(response.Data?.Id, "job");
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

        protected static string? PromptSelection(IEnumerable<string>? items, string title, bool writeSelection = true)
        {
            return CliPromptUtil.PromptSelection(items, title, writeSelection);
        }

        protected static CliSelectItem<T>? PromptSelection<T>(IEnumerable<CliSelectItem<T>>? items, string title)
        {
            return CliPromptUtil.PromptSelection(items, title);
        }

        protected static TEnum PromptSelection<TEnum>(string title)
            where TEnum : struct, Enum
        {
            var items = Enum.GetNames<TEnum>().Select(e => e.ToLower());
            var result = CliPromptUtil.PromptSelection(items, title);
            return Enum.Parse<TEnum>(result!, true);
        }

        protected static bool ConfirmAction(string title)
        {
            if (!InteractiveMode) { return true; }
            return AnsiConsole.Confirm($"are you sure that you want to {title}?", false);
        }

        protected static int GetCounterHours()
        {
            var items = new[] { "1 hour", "2 hours", "8 hours", "1 day", "2 days", "3 days", "7 days" };
            var select = PromptSelection(items, "select time period");
            if (string.IsNullOrEmpty(select)) { return 0; }
            var parts = select.Split(' ');
            if (parts.Length != 2) { return 0; }
            if (!int.TryParse(parts[0], out var num)) { return 0; }
            if (parts[1].Length < 1) { return 0; }
            if (parts[1][0] == 'h') { return num; }
            if (parts[1][0] == 'd') { return num * 24; }
            return 0;
        }

        protected static void FillDatesScope(ICliDateScope request)
        {
            if (request.FromDate == default && request.ToDate == default)
            {
                var dates = GetDateScope();
                request.FromDate = dates.Item1 ?? default;
                request.ToDate = dates.Item2 ?? default;
            }
        }

        private static (DateTime?, DateTime?) GetDateScope()
        {
            var items = new[] { "today", "yesterday", "this week", "last week", "this month", "last month", "this year", "last year", "since forever", "custom..." };
            var select = PromptSelection(items, "select date period");

            if (string.Equals(select, "custom...", StringComparison.OrdinalIgnoreCase))
            {
                return GetCustomDateScope();
            }

            (DateTime?, DateTime?) result = select switch
            {
                "today" => (DateTime.Today, null),
                "yesterday" => (DateTime.Today.AddDays(-1), DateTime.Today),
                "this week" => (DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek), DateTime.Today.AddDays(7 - (int)DateTime.Today.DayOfWeek)),
                "last week" => (DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek - 7), DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek)),
                "this month" => (DateTime.Today.AddDays(-DateTime.Today.Day + 1), DateTime.Today.AddDays(-DateTime.Today.Day + 1).AddMonths(1)),
                "last month" => (DateTime.Today.AddDays(-DateTime.Today.Day + 1).AddMonths(-1), DateTime.Today.AddDays(-DateTime.Today.Day + 1)),
                "this year" => (DateTime.Today.AddDays(-DateTime.Today.DayOfYear + 1), DateTime.Today.AddDays(-DateTime.Today.DayOfYear + 1).AddYears(1)),
                "last year" => (DateTime.Today.AddDays(-DateTime.Today.DayOfYear + 1).AddYears(-1), DateTime.Today.AddDays(-DateTime.Today.DayOfYear + 1)),
                "since forever" => (null, null),
                _ => (null, null)
            };

            PrintSummaryDate("from date:", result.Item1);
            PrintSummaryDate("to date:  ", result.Item2);

            return result;
        }

        private static void PrintSummaryDate(string title, DateTime? date)
        {
            if (date == null)
            {
                AnsiConsole.MarkupLine($"[turquoise2]  > {title.EscapeMarkup()} [/] [[empty]]");
            }
            else
            {
                var format = CliActionMetadata.GetCurrentDateTimeFormat();
                var value = date.Value.ToString(format);
                AnsiConsole.MarkupLine($"[turquoise2]  > {title.EscapeMarkup()} [/] {value}");
            }
        }

        private static (DateTime?, DateTime?) GetCustomDateScope()
        {
            var from = CliPromptUtil.PromptForDate("from date");
            var to = CliPromptUtil.PromptForDate("to date  ");
            return (from, to);
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

            foreach (var act in allActions)
            {
                var actionAttributes = act.GetCustomAttributes<ActionAttribute>();
                var nullRequestAttribute = act.GetCustomAttribute<NullRequestAttribute>();
                var ignoreHelpAttribute = act.GetCustomAttribute<IgnoreHelpAttribute>();

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
                };

                if (!string.IsNullOrEmpty(moduleAttribute?.Synonyms))
                {
                    item.ModuleSynonyms = [.. moduleAttribute.Synonyms.Split(',')];
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

            var last = parameters[^1];
            if (last.ParameterType != typeof(CancellationToken))
            {
                throw new CliException($"cli error: action '{method.Name}' has no CancellationToken parameter");
            }

            var requestType =
                parameters.Length == 1 ?
                null :
                parameters[0].ParameterType;

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