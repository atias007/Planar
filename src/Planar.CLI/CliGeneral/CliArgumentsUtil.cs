using Planar.CLI.Actions;
using Planar.CLI.Attributes;
using Planar.CLI.CliGeneral;
using Planar.CLI.Exceptions;
using Planar.CLI.General;
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

namespace Planar.CLI
{
    public class CliArgumentsUtil
    {
        private const string OutputTerm = "--inner-cli-output-filename";
        private const string RegexTemplate = "^[1-9][0-9]{0,18}$";
        private static readonly Regex _historyRegex = new(RegexTemplate, RegexOptions.Compiled, TimeSpan.FromSeconds(2));
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
            if (!_outputFilename.Contains('.')) { _outputFilename = $"{_outputFilename}.txt"; }
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
                return CliArguments.Any(a =>
                string.Equals(a.Key, $"-{IterativeActionPropertyAttribute.ShortNameText}", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(a.Key, $"--{IterativeActionPropertyAttribute.LongNameText}", StringComparison.OrdinalIgnoreCase));
            }
        }

        public string Module { get; set; }

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

        public static CliActionMetadata? ValidateArgs(ref string[] args, IEnumerable<CliActionMetadata> actionsMetadata)
        {
            var list = args.ToList();

            // special case: enable get history only by type history id
            if (_historyRegex.IsMatch(list[0]))
            {
                list.Insert(0, "history");
                list.Insert(1, "get");
            }

            // find match
            var action = FindMatch(list, actionsMetadata);
            args = list.ToArray();
            if (action != null) { return action; }

            // find match with swap command and module
            Swap(ref list);
            args = list.ToArray();
            action = FindMatch(list, actionsMetadata);
            if (action != null) { return action; }
            Swap(ref list);

            // special case: enable to list jobs only by type ls or list
            if (list[0].ToLower() == "ls") { list.Insert(0, "job"); }
            if (list[0].ToLower() == "list") { list.Insert(0, "job"); }

            // module not found
            if (!actionsMetadata.Any(a => a.Module.ToLower() == list[0].ToLower()))
            {
                var modules = GetModuleByCommand(list[0], actionsMetadata);
                if (!modules.Any())
                {
                    throw new CliValidationException($"module '{list[0]}' is not supported");
                }

                if (modules.Count() > 1)
                {
                    var commands = string.Join(',', modules);
                    throw new CliValidationException($"module '{list[0]}' is not supported\r\nthe following modules has command '{list[0].Trim()}':\r\n{commands}");
                }

                list.Insert(0, modules.First());
            }

            if (list.Count == 1)
            {
                var message = $"missing command for module '{list[0]}'\r\ncommand line format is {{0}}'<module> <command> [<options>]'";
                var cliCommand = BaseCliAction.InteractiveMode ? string.Empty : $"{CliHelpGenerator.CliCommand} ";
                var final = string.Format(message, cliCommand);
                throw new CliValidationException(final);
            }

            // command not found
            if (!actionsMetadata.Any(a => a.Commands.Any(c => c?.ToLower() == list[1].ToLower())))
            {
                // help command
                if (IsHelpCommand(list[1]))
                {
                    CliHelpGenerator.ShowHelp(list[0], actionsMetadata);
                    return null;
                }

                throw new CliValidationException($"module '{list[0]}' does not support command '{list[1]}'");
            }

            args = list.ToArray();
            action = FindMatch(list, actionsMetadata);
            if (action == null)
            {
                throw new CliValidationException($"module '{list[0]}' does not support command '{list[1]}'");
            }

            return action;
        }

        public object? GetRequest(CliActionMetadata action, CancellationToken cancellationToken)
        {
            if (!CliArguments.Any() && action.AllowNullRequest) { return null; }
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

            metadata = action.Arguments.FirstOrDefault(m => m.IsJobOrTriggerKey);
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

            var allRequired = action.Arguments
                .Where(a => a.MissingRequired)
                .OrderBy(a => a.DefaultOrder);

            foreach (var item in allRequired)
            {
                ValidateMissingRequiredProperties(item);
            }

            return result;
        }

        private static void IsValidFilePath(string? outputFilename)
        {
            if (outputFilename == null) { return; }

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
            if (metadata.JobKey && arg.Value == "?")
            {
                var jobId = await JobCliActions.ChooseJob(cancellationToken);
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
                    if (arg.Value != "?")
                    {
                        Util.LastJobOrTriggerId = arg.Value;
                    }
                }
            }
        }

        private static async Task FillTriggerId(CliArgumentMetadata prop, CliArgument arg)
        {
            if (prop.TriggerKey && arg.Value == "?")
            {
                var triggerId = await JobCliActions.ChooseTrigger();
                arg.Value = triggerId;
                Util.LastJobOrTriggerId = triggerId;
            }
        }

        private static CliActionMetadata? FindMatch(List<string> args, IEnumerable<CliActionMetadata> actionsMetadata)
        {
            var moduleExists = actionsMetadata.Any(a => a.Module == args[0].ToLower());
            if (args.Count == 1 && moduleExists)
            {
                args.Add("--help");
                return null;
            }

            var moduleSynonymExists = actionsMetadata.Any(a => a.ModuleSynonyms.Any(s => s == args[0].ToLower()) && a.Commands.Contains("list"));
            if (args.Count == 1 && moduleSynonymExists)
            {
                args.Add("list");
            }

            if (args.Count < 2) { return null; }

            var action = actionsMetadata.FirstOrDefault(a =>
                a.ModuleSynonyms.Any(s => s == args[0].ToLower()) &&
                a.Commands.Any(c => c?.ToLower() == args[1].ToLower()));

            return action;
        }

        private static IEnumerable<string> GetModuleByCommand(string subArgument, IEnumerable<CliActionMetadata> cliActionsMetadata)
        {
            var metadata = cliActionsMetadata
                .Where(m => m.Commands.Any(c => c?.ToLower() == subArgument.ToLower()))
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
                matchProp = props.FirstOrDefault(p => p.LongName == key);
            }
            else if (a.Key != null && a.Key.StartsWith("-"))
            {
                var key = a.Key[1..].ToLower();
                matchProp = props.FirstOrDefault(p => p.ShortName == key);
            }
            else
            {
                var defaultOrder = startDefaultOrder;
                matchProp = props
                    .FirstOrDefault(p => p.Default && (p.DefaultOrder == defaultOrder));

                startDefaultOrder++;

                a.Value = a.Key;
            }

            if (matchProp == null)
            {
                throw new CliValidationException($"argument '{a.Key}' is not supported with command '{action.CommandsTitle}' at module '{action.Module}'");
            }

            if (a.Key != null && a.Key.StartsWith("-") && string.IsNullOrEmpty(a.Value))
            {
                a.Value = true.ToString();
            }

            return matchProp;
        }

        private static void SetValue(PropertyInfo? prop, object? instance, string? value)
        {
            if (prop == null) { return; }
            if (instance == null) { return; }

            if (string.Equals(value, prop.Name, StringComparison.OrdinalIgnoreCase) && prop.PropertyType == typeof(bool))
            {
                prop.SetValue(instance, true);
                return;
            }

            try
            {
                object? objValue = value;
                if (value != null && prop.PropertyType.BaseType == typeof(Enum))
                {
                    objValue = ParseEnum(prop.PropertyType, value);
                }

                if (value != null &&
                    prop.PropertyType.GenericTypeArguments.Length == 1 &&
                    prop.PropertyType.GenericTypeArguments[0].BaseType == typeof(Enum))
                {
                    objValue = ParseEnum(prop.PropertyType.GenericTypeArguments[0], value);
                    prop.SetValue(instance, objValue);
                    return;
                }

                prop.SetValue(instance, Convert.ChangeType(objValue, prop.PropertyType, CultureInfo.CurrentCulture));
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
}