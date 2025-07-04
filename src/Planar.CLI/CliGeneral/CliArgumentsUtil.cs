﻿using Planar.CLI.Actions;
using Planar.CLI.Attributes;
using Planar.CLI.CliGeneral;
using Planar.CLI.Exceptions;
using Planar.CLI.General;
using Planar.Common;
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

namespace Planar.CLI;

public class CliArgumentsUtil
{
    private const string OutputTerm = "--inner-cli-output-filename";
    private const string NumericRegexTemplate = "^[1-9][0-9]{0,18}$";
    private const string JobIdRegexTemplate = "^[a-z0-9]{11}$";
    private const string InstanceIdRegexTemplate = "^[A-Za-z0-9_-]{3,50}[0-9]{1,18}$";

    private static readonly Regex _numericRegex = new(NumericRegexTemplate, RegexOptions.Compiled, TimeSpan.FromSeconds(1));
    private static readonly Regex _jobIdRegex = new(JobIdRegexTemplate, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
    private static readonly Regex _instanceIdRegex = new(InstanceIdRegexTemplate, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
    private readonly string? _outputFilename;

    public CliArgumentsUtil(string[] args)
    {
        Module = args[0];
        Command = args[1];

        var list = new List<CliArgument>();
        for (int i = 2; i < args.Length; i++)
        {
            if (args[i] == ">") { args[i] = OutputTerm; }
            list.Add(new CliArgument { Key = args[i] });
        }

        for (int i = 1; i < list.Count; i++)
        {
            var item1 = list[i - 1];
            var item2 = list[i];

            if (IsKeyArgument(item1) && !IsKeyArgument(item2))
            {
                item1.Value = item2.Key;
                item2.Key = null;
                i++;
            }
        }

        foreach (var item in list)
        {
            if (item.Key != null && string.IsNullOrEmpty(item.Value))
            {
                item.Value = true.ToString();
            }
        }

        CliArguments = list.Where(l => l.Key != null).ToList();
        _outputFilename = CliArguments
            .Where(a => a.Key == OutputTerm)
            .Select(a => a.Value)
            .FirstOrDefault();

        _outputFilename = _outputFilename?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(_outputFilename) && !_outputFilename.Contains('.')) { _outputFilename = $"{_outputFilename}.txt"; }
        IsValidFilePath(_outputFilename);

        CliArguments.RemoveAll(a => a.Key == OutputTerm);
    }

    public List<CliArgument> CliArguments { get; set; }

    public Type? RequestType { get; set; }

    public string? OutputFilename => _outputFilename;

    public string Command { get; set; }

    public bool HasIterativeArgument
    {
        get
        {
            return CliArguments.Exists(a =>
            string.Equals(a.Key, $"-{IterativeActionPropertyAttribute.ShortNameText}", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(a.Key, $"--{IterativeActionPropertyAttribute.LongNameText}", StringComparison.OrdinalIgnoreCase));
        }
    }

    public string Module { get; set; }

    private static KeyValuePair<string, string> ParseKeyValuePair(string value)
    {
        var index = value.IndexOf('=');
        if (index == -1)
        {
            throw new CliException($"value '{value}' is invalid. value must be in format <key>=<value>");
        }

        var key = value[..index];
        var val = value.Length <= index + 1 ? string.Empty : value[(index + 1)..];
        return new KeyValuePair<string, string>(key, val ?? string.Empty);
    }

    public static void AddDataToDictionary(PropertyInfo propertyInfo, object instance, string value)
    {
        if (propertyInfo.GetValue(instance) is not Dictionary<string, string> dictionary)
        {
            dictionary = [];
        }

        var kvp = ParseKeyValuePair(value);
        dictionary.Add(kvp.Key, kvp.Value);
        propertyInfo.SetValue(instance, dictionary);
    }

    public static object? ParseEnum(Type type, string? value)
    {
        if (value == null) { return null; }

        try
        {
            var result = Enum.Parse(type, value, true);
            return result;
        }
        catch (ArgumentException)
        {
            if (value.Contains('-'))
            {
                value = value.Replace("-", string.Empty);
                return ParseEnum(type, value);
            }

            var values = string.Join(',', Enum.GetNames(type));
            throw new CliException($"value '{value}' is invalid. available values for this argument is: {values.ToLower()}");
        }
    }

    private static async Task SpecialCase(List<string> args)
    {
        // SPECIAL CASE: accept command with planar keyword
        if (string.Equals(args[0], "planar-cli", StringComparison.OrdinalIgnoreCase))
        {
            args.RemoveAt(0);
        }

        // SPECIAL CASE: enable get history only by type history id
        if (_numericRegex.IsMatch(args[0]))
        {
            args.Insert(0, "history");
            args.Insert(1, "get");
            return;
        }

        // SPECIAL CASE: job id / trigger id
        if (_jobIdRegex.IsMatch(args[0]))
        {
            var idType = await JobTriggerIdResolver.SafeGetIdType(args[0]);
            if (idType == IdType.JobId)
            {
                args.Insert(0, "job");
                args.Insert(1, "get");
                return;
            }
            else if (idType == IdType.TriggerId)
            {
                args.Insert(0, "trigger");
                args.Insert(1, "get");
                return;
            }
        }

        // SPECIAL CASE: instance id
        if (_instanceIdRegex.IsMatch(args[0]))
        {
            args.Insert(0, "history");
            args.Insert(1, "get");
            return;
        }
    }

    public async static Task<(CliActionMetadata? Action, string[] Args)> ValidateArgs(string[] args, IEnumerable<CliActionMetadata> actionsMetadata)
    {
        var list = args.ToList();
        await SpecialCase(list);

        // find match
        var action = FindMatch(list, actionsMetadata);
        args = [.. list];
        if (action != null && action.Module != InnerCliActions.Command) { return (action, args); }

        // find match with swap command and module
        Swap(ref list);
        args = [.. list];
        action = FindMatch(list, actionsMetadata);
        if (action != null) { return (action, args); }
        Swap(ref list);

        // special case: enable to list jobs only by type ls or list
        if (list[0].Equals("ls", StringComparison.CurrentCultureIgnoreCase)) { list.Insert(0, "job"); }
        if (list[0].Equals("list", StringComparison.CurrentCultureIgnoreCase)) { list.Insert(0, "job"); }

        // inner
        var inner = string.Equals(list[0], InnerCliActions.Command, StringComparison.CurrentCultureIgnoreCase);
        if (inner)
        {
            throw new CliValidationException($"module '{list[0]}' is not supported");
        }

        // module not found
        var any = actionsMetadata.Any(a => a.Module.Equals(list[0], StringComparison.CurrentCultureIgnoreCase));
        if (!any)
        {
            var modules = GetModuleByCommand(list[0], actionsMetadata);
            if (!modules.Any())
            {
                throw new CliValidationException($"module '{list[0]}' is not supported");
            }

            if (modules.Count() > 1)
            {
                var commands = string.Join($"\r\n {CliTableFormat.bullet} ", modules);
                throw new CliValidationException($"module '{list[0]}' is not supported", $"the following modules has command '{list[0].Trim()}':\r\n {CliTableFormat.bullet} {commands}");
            }

            list.Insert(0, modules.First());
        }

        if (list.Count == 1)
        {
            var message = $"command line format is {{0}}'<module> <command> [<options>]'";
            var cliCommand = BaseCliAction.InteractiveMode ? string.Empty : $"{CliHelpGenerator.CliCommand} ";
            var final = string.Format(message, cliCommand);
            throw new CliValidationException($"missing command for module '{list[0]}'", final);
        }

        // command not found
        if (!actionsMetadata.Any(a => a.Commands.Exists(c => string.Equals(c, list[1], StringComparison.OrdinalIgnoreCase)))) //// c?.ToLower() == list[1].ToLower())))
        {
            // help command
            if (IsHelpCommand(list[1]))
            {
                CliHelpGenerator.ShowHelp(list[0], actionsMetadata);
                return (null, args);
            }

            throw new CliValidationException($"module '{list[0]}' does not support command '{list[1]}'");
        }

        args = [.. list];
        action = FindMatch(list, actionsMetadata);
        if (action == null)
        {
            throw new CliValidationException($"module '{list[0]}' does not support command '{list[1]}'");
        }

        return (action, args);
    }

    public object? GetRequest(CliActionMetadata action, CancellationToken cancellationToken)
    {
        if (CliArguments.Count == 0 && action.AllowNullRequest) { return null; }
        if (action.RequestType == null) { return null; }

        var result = Activator.CreateInstance(action.RequestType);
        var defaultProps = action.Arguments.Where(p => p.Default);
        var startDefaultOrder = defaultProps.Any() ? defaultProps.Min(p => p.DefaultOrder) : -1;

        CliArgumentMetadata? metadata;
        foreach (var arg in CliArguments)
        {
            metadata = MatchProperty(action, action.Arguments, ref startDefaultOrder, arg);
            FillJobOrTrigger(arg, metadata, cancellationToken);
            SetValue(metadata.PropertyInfo, result, arg.Value);
            if (!string.IsNullOrEmpty(arg.Value))
            {
                metadata.ValueSupplied = true;
            }
        }

        metadata = action.Arguments.Find(m => m.IsJobOrTriggerKey);
        if (metadata != null)
        {
            var value = metadata.PropertyInfo?.GetValue(result);
            var strValue = Convert.ToString(value);
            if (strValue == null || strValue == string.Empty)
            {
                var arg = new CliArgument { Key = "?", Value = "?" };
                FillJobOrTrigger(arg, metadata, cancellationToken);
                SetValue(metadata.PropertyInfo, result, arg.Value);
                if (!string.IsNullOrEmpty(arg.Value))
                {
                    metadata.ValueSupplied = true;
                }
            }
        }

        // Validate requierd properties
        if (!BaseCliAction.InteractiveMode)
        {
            var allRequired = action.Arguments
                .Where(a => a.MissingRequired)
                .OrderBy(a => a.DefaultOrder);

            foreach (var item in allRequired)
            {
                ValidateMissingRequiredProperties(item);
            }
        }

        return result;
    }

    private static void IsValidFilePath(string? outputFilename)
    {
        if (string.IsNullOrEmpty(outputFilename)) { return; }

        try
        {
            // Check if the file name with path is valid
            var directory = Path.GetDirectoryName(outputFilename);
            var fileName = Path.GetFileName(outputFilename);

            // Validate the directory and file name separately
            bool isDirectoryValid = string.IsNullOrEmpty(directory) || Directory.Exists(directory);
            bool isFileNameValid = !string.IsNullOrEmpty(fileName) && fileName.IndexOfAny(Path.GetInvalidFileNameChars()) == -1;

            if (!isDirectoryValid)
            {
                throw new CliValidationException($"output directory '{directory}' is not exists");
            }

            if (!isFileNameValid)
            {
                throw new CliValidationException($"output filename '{fileName}' is invalid");
            }
        }
        catch (CliValidationException)
        {
            throw;
        }
        catch (Exception)
        {
            // Handle any exception that occurs during the validation
            throw new CliValidationException($"output filename '{outputFilename}' is invalid");
        }
    }

    private static async Task FillJobId(CliArgumentMetadata metadata, CliArgument arg, CancellationToken cancellationToken)
    {
        if (metadata.JobKey && arg.Value.HasValue() && arg.Value.StartsWith('?'))
        {
            var value = arg.Value;
            var withGroup = value.StartsWith("??");
            value = withGroup ? value[2..] : value[1..];
            var filter = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            var jobId = await JobCliActions.ChooseJob(filter, withGroup, writeSelection: true, cancellationToken);
            arg.Value = jobId;
            if (arg.Key == "?")
            {
                arg.Key = jobId;
            }

            Util.LastJobOrTriggerId = jobId;
        }
    }

    private static void FillJobOrTrigger(CliArgument a, CliArgumentMetadata metadata, CancellationToken cancellationToken)
    {
        FillLastJobOrTriggerId(metadata, a);

        if (metadata.JobKey)
        {
            FillJobId(metadata, a, cancellationToken).Wait(cancellationToken);
        }
        else if (metadata.TriggerKey)
        {
            FillTriggerId(metadata, a).Wait(cancellationToken);
        }
    }

    private static void FillLastJobOrTriggerId(CliArgumentMetadata prop, CliArgument arg)
    {
        if (prop.IsJobOrTriggerKey)
        {
            if (arg.Value == "!")
            {
                arg.Value = Util.LastJobOrTriggerId;
            }
            else
            {
                if (string.IsNullOrEmpty(arg.Value) || !arg.Value.StartsWith('?'))
                {
                    Util.LastJobOrTriggerId = arg.Value;
                }
            }
        }
    }

    private static async Task FillTriggerId(CliArgumentMetadata prop, CliArgument arg)
    {
        if (prop.TriggerKey && arg.Value.HasValue() && arg.Value.StartsWith('?'))
        {
            var filter = arg.Value.Length == 1 ? null : arg.Value[1..].Trim();
            var triggerId = await JobCliActions.ChooseTrigger(filter);
            arg.Value = triggerId;
            Util.LastJobOrTriggerId = triggerId;
        }
    }

    private static CliActionMetadata? FindMatch(List<string> args, IEnumerable<CliActionMetadata> actionsMetadata)
    {
        var moduleExists = actionsMetadata.Any(a => a.Module.Equals(args[0], StringComparison.CurrentCultureIgnoreCase));
        if (args.Count == 1 && moduleExists)
        {
            args.Add("--help");
            return null;
        }

        var moduleSynonymExists = actionsMetadata.Any(a => a.ModuleSynonyms.Exists(s => s.Equals(args[0], StringComparison.CurrentCultureIgnoreCase)) && a.Commands.Contains("list"));
        if (args.Count == 1 && moduleSynonymExists)
        {
            args.Add("list");
        }

        if (args.Count < 2) { return null; }

        var action = actionsMetadata.FirstOrDefault(a =>
            a.ModuleSynonyms.Exists(s => s.Equals(args[0], StringComparison.CurrentCultureIgnoreCase)) &&
            a.Commands.Exists(c => c.Equals(args[1], StringComparison.CurrentCultureIgnoreCase)));

        return action;
    }

    private static IEnumerable<string> GetModuleByCommand(string subArgument, IEnumerable<CliActionMetadata> cliActionsMetadata)
    {
        var metadata = cliActionsMetadata
            .Where(m => m.Commands.Exists(c => c.Equals(subArgument, StringComparison.CurrentCultureIgnoreCase)))
            .Select(m => m.Module);

        return metadata;
    }

    private static bool IsHelpCommand(string command)
    {
        if (string.IsNullOrEmpty(command)) { return false; }
        var cmd = command.ToLower();
        if (cmd == "help") { return true; }
        if (cmd == "-h") { return true; }
        if (cmd == "--help") { return true; }
        return false;
    }

    private static bool IsKeyArgument(CliArgument arg)
    {
        if (string.IsNullOrEmpty(arg.Key)) { return false; }
        const string template = "^-(-?)[a-z,A-Z]";
        return Regex.IsMatch(arg.Key, template, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
    }

    private static CliArgumentMetadata MatchProperty(CliActionMetadata action, List<CliArgumentMetadata> props, ref int startDefaultOrder, CliArgument a)
    {
        CliArgumentMetadata? matchProp = null;
        if (a.Key != null && a.Key.StartsWith("--"))
        {
            var key = a.Key[2..].ToLower();
            matchProp = props.Find(p => p.LongName == key);
        }
        else if (a.Key != null && a.Key.StartsWith('-'))
        {
            var key = a.Key[1..].ToLower();
            matchProp = props.Find(p => p.ShortName == key);
        }
        else
        {
            var defaultOrder = startDefaultOrder;
            matchProp = props
                .Find(p => p.Default && (p.DefaultOrder == defaultOrder));

            startDefaultOrder++;

            a.Value = a.Key;
        }

        if (matchProp == null)
        {
            throw new CliValidationException($"argument '{a.Key}' is not supported with command '{action.CommandsTitle}' at module '{action.Module}'");
        }

        if (a.Key != null && a.Key.StartsWith('-') && string.IsNullOrEmpty(a.Value))
        {
            a.Value = true.ToString();
        }

        return matchProp;
    }

    private static void SetValue(PropertyInfo? prop, object? instance, string? value)
    {
        if (prop == null) { return; }
        if (instance == null) { return; }

        try
        {
            // bool data type
            if (string.Equals(value, prop.Name, StringComparison.OrdinalIgnoreCase) &&
                prop.PropertyType == typeof(bool))
            {
                prop.SetValue(instance, true);
                return;
            }

            // Enum data type
            object? objValue = value;
            if (value != null && prop.PropertyType.BaseType == typeof(Enum))
            {
                objValue = ParseEnum(prop.PropertyType, value);
            }

            // Nullable Enum data type
            if (value != null &&
                prop.PropertyType.GenericTypeArguments.Length == 1 &&
                prop.PropertyType.GenericTypeArguments[0].BaseType == typeof(Enum))
            {
                objValue = ParseEnum(prop.PropertyType.GenericTypeArguments[0], value);
                prop.SetValue(instance, objValue);
                return;
            }

            // TimeSpan data type
            if (value != null && prop.PropertyType.Is<TimeSpan>())
            {
                objValue = ParseTimeSpan(value);
                prop.SetValue(instance, objValue);
                return;
            }

            if (value != null && prop.PropertyType == typeof(Dictionary<string, string>))
            {
                AddDataToDictionary(prop, instance, value);
                return;
            }

            // Check for nullable types
            var nullableType = Nullable.GetUnderlyingType(prop.PropertyType);
            if (nullableType == null)
            {
                prop.SetValue(instance, Convert.ChangeType(objValue, prop.PropertyType, CultureInfo.CurrentCulture));
            }
            else
            {
                prop.SetValue(instance, Convert.ChangeType(objValue, nullableType, CultureInfo.CurrentCulture));
            }
        }
        catch (CliException)
        {
            throw;
        }
        catch (Exception)
        {
            var attribute = prop.GetCustomAttribute<ActionPropertyAttribute>();
            attribute ??= new ActionPropertyAttribute();

            var message = $"value '{value}' has wrong format for argument {attribute.DisplayName}";

            throw new CliException(message);
        }
    }

    private static TimeSpan ParseTimeSpan(string value)
    {
        if (TimeSpan.TryParse(value, CultureInfo.CurrentCulture, out var result))
        {
            return result;
        }

        if (DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out var dateTime))
        {
            if (dateTime < DateTime.Now) { return TimeSpan.Zero; }
            return dateTime - DateTime.Now;
        }

        var ts = ParseTimeSpanPharse(value);
        if (ts != null && ts != TimeSpan.Zero) { return ts.Value; }

        throw new InvalidCastException(value);
    }

    private static TimeSpan? ParseTimeSpanPharse(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) { return null; }
        value = value.Trim().ToLower();

        var sec = GetCleanValues(value, "sec", "second", "seconds");
        if (sec != null) { return TimeSpan.FromSeconds(sec.Value); }

        var min = GetCleanValues(value, "min", "minute", "minutes");
        if (min != null) { return TimeSpan.FromMinutes(min.Value); }

        var hour = GetCleanValues(value, "hour", "hours");
        if (hour != null) { return TimeSpan.FromHours(hour.Value); }

        var day = GetCleanValues(value, "day", "days");
        if (day != null) { return TimeSpan.FromHours(day.Value); }

        return null;

        static long? GetCleanValues(string value, params string[] pharses)
        {
            foreach (var p in pharses)
            {
                var result = GetCleanValue(value, p);
                if (result.HasValue) { return result; }
            }

            return null;
        }

        static long? GetCleanValue(string value, string pharse)
        {
            var relevant = value.EndsWith(pharse);
            if (!relevant) { return null; }

            var cleanValue = value.Replace(pharse, string.Empty);
            if (!long.TryParse(cleanValue, out var longValue)) { return null; }
            return longValue;
        }
    }

    private static void Swap(ref List<string> args)
    {
        if (args.Count < 2) { return; }
        (args[1], args[0]) = (args[0], args[1]);
    }

    private static void ValidateMissingRequiredProperties(CliArgumentMetadata props)
    {
        var message =
            string.IsNullOrEmpty(props.RequiredMissingMessage) ?
            $"argument {props.Name} is required" :
            props.RequiredMissingMessage;

        throw new CliException(message);
    }
}