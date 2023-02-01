using Planar.CLI.Actions;
using Planar.CLI.Attributes;
using Planar.CLI.Exceptions;
using Planar.CLI.General;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Planar.CLI
{
    public class CliArgumentsUtil
    {
        private const string RegexTemplate = "^[1-9][0-9]*$";
        private static readonly Regex _historyRegex = new Regex(RegexTemplate, RegexOptions.Compiled, TimeSpan.FromSeconds(2));

        public CliArgumentsUtil(string[] args)
        {
            Module = args[0];
            Command = args[1];

            var list = new List<CliArgument>();
            for (int i = 2; i < args.Length; i++)
            {
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
        }

        private static bool IsKeyArgument(CliArgument arg)
        {
            if (string.IsNullOrEmpty(arg.Key)) { return false; }
            const string template = "^-(-?)[a-z,A-Z]";
            return Regex.IsMatch(arg.Key, template, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
        }

        private static IEnumerable<string> GetModuleByArgument(string subArgument, IEnumerable<CliActionMetadata> cliActionsMetadata)
        {
            var metadata = cliActionsMetadata
                .Where(m => m.Command.Any(c => c?.ToLower() == subArgument.ToLower()))
                .Select(m => m.Module);

            return metadata;
        }

        public static CliActionMetadata ValidateArgs(ref string[] args, IEnumerable<CliActionMetadata> cliActionsMetadata)
        {
            var list = args.ToList();

            if (list[0].ToLower() == "ls") { list.Insert(0, "job"); }

            if (_historyRegex.IsMatch(list[0]))
            {
                list.Insert(0, "history");
                list.Insert(1, "get");
            }

            if (!cliActionsMetadata.Any(a => a.Module.ToLower() == list[0].ToLower()))
            {
                var modules = GetModuleByArgument(list[0], cliActionsMetadata);
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
                throw new CliValidationException($"missing command for module '{list[0]}'\r\n. command line format is 'planar-cli <module> <command> [<options>]'");
            }

            if (!cliActionsMetadata.Any(a => a.Command.Any(c => c?.ToLower() == list[1].ToLower())))
            {
                throw new CliValidationException($"module '{list[0]}' does not support command '{list[1]}'");
            }

            var action = cliActionsMetadata.FirstOrDefault(a => a.Command.Any(c => c?.ToLower() == list[1].ToLower() && a.Module == list[0].ToLower()));
            if (action == null)
            {
                throw new CliValidationException($"module '{list[0]}' does not support command '{list[1]}'");
            }

            args = list.ToArray();
            return action;
        }

        public string Module { get; set; }

        public string Command { get; set; }

        public List<CliArgument> CliArguments { get; set; } = new List<CliArgument>();

        public object? GetRequest(Type type, CliActionMetadata action)
        {
            if (!CliArguments.Any() && action.AllowNullRequest) { return null; }

            var result = Activator.CreateInstance(type);
            var props = action.GetRequestPropertiesInfo();

            var defaultProps = props.Where(p => p.Default);
            var startDefaultOrder = defaultProps.Any() ? defaultProps.Min(p => p.DefaultOrder) : -1;

            foreach (var a in CliArguments)
            {
                var matchProp = MatchProperty(action, props, ref startDefaultOrder, a);
                FillJobOrTrigger(action, a, matchProp);
                SetValue(matchProp.PropertyInfo, result, a.Value);
                if (!string.IsNullOrEmpty(a.Value))
                {
                    matchProp.ValueSupplied = true;
                }
            }

            FindMissingRequiredProperties(props);

            return result;
        }

        private static void FillJobOrTrigger(CliActionMetadata action, CliArgument a, RequestPropertyInfo matchProp)
        {
            FillLastJobOrTriggerId(matchProp, a);
            if (action.Module?.ToLower() == "job")
            {
                FillJobId(matchProp, a).Wait();
            }
            else if (action.Module?.ToLower() == "trigger")
            {
                FillTriggerId(matchProp, a).Wait();
            }
        }

        private static RequestPropertyInfo MatchProperty(CliActionMetadata action, List<RequestPropertyInfo> props, ref int startDefaultOrder, CliArgument a)
        {
            RequestPropertyInfo? matchProp = null;
            if (a.Key.StartsWith("--"))
            {
                var key = a.Key[2..].ToLower();
                matchProp = props.FirstOrDefault(p => p.LongName == key);
            }
            else if (a.Key.StartsWith("-"))
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
                throw new CliValidationException($"argument '{a.Key}' is not supported with command '{action.Command.FirstOrDefault()}' at module '{action.Module}'");
            }

            if (a.Key.StartsWith("-") && string.IsNullOrEmpty(a.Value))
            {
                a.Value = true.ToString();
            }

            return matchProp;
        }

        public bool HasIterativeArgument
        {
            get
            {
                return CliArguments.Any(a =>
                string.Equals(a.Key, $"-{IterativeActionPropertyAttribute.ShortNameText}", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(a.Key, $"--{IterativeActionPropertyAttribute.LongNameText}", StringComparison.OrdinalIgnoreCase));
            }
        }

        private static async Task FillJobId(RequestPropertyInfo prop, CliArgument arg)
        {
            if (prop.JobOrTriggerKey && arg.Value == "?")
            {
                var jobId = await JobCliActions.ChooseJob();
                arg.Value = jobId;
                Util.LastJobOrTriggerId = jobId;
            }
        }

        private static async Task FillTriggerId(RequestPropertyInfo prop, CliArgument arg)
        {
            if (prop.JobOrTriggerKey && arg.Value == "?")
            {
                var triggerId = await JobCliActions.ChooseTrigger();
                arg.Value = triggerId;
                Util.LastJobOrTriggerId = triggerId;
            }
        }

        private static void FillLastJobOrTriggerId(RequestPropertyInfo prop, CliArgument arg)
        {
            if (prop.JobOrTriggerKey)
            {
                if (arg.Value == "!!")
                {
                    arg.Value = Util.LastJobOrTriggerId;
                }
                else
                {
                    Util.LastJobOrTriggerId = arg.Value;
                }
            }
        }

        private static void FindMissingRequiredProperties(IEnumerable<RequestPropertyInfo> props)
        {
            var p = props.FirstOrDefault(v => v.Required && !v.ValueSupplied);
            if (p != null)
            {
                var message =
                    string.IsNullOrEmpty(p.RequiredMissingMessage) ?
                    $"argument {p.Name} is required" :
                    p.RequiredMissingMessage;

                throw new CliException(message);
            }
        }

        private static void SetValue(PropertyInfo prop, object instance, string value)
        {
            if (string.Equals(value, prop.Name, StringComparison.OrdinalIgnoreCase) && prop.PropertyType == typeof(bool))
            {
                prop.SetValue(instance, true);
                return;
            }

            try
            {
                object objValue = value;
                if (prop.PropertyType.BaseType == typeof(Enum))
                {
                    objValue = ParseEnum(prop.PropertyType, value);
                }
                prop.SetValue(instance, Convert.ChangeType(objValue, prop.PropertyType, CultureInfo.CurrentCulture));
            }
            catch (Exception)
            {
                var attribute = prop.GetCustomAttribute<ActionPropertyAttribute>();
                attribute ??= new ActionPropertyAttribute();

                var message = $"value '{value}' has wrong format for argument {attribute.DisplayName}";

                throw new CliException(message);
            }
        }

        private static object ParseEnum(Type type, string value)
        {
            try
            {
                var result = Enum.Parse(type, value, true);
                return result;
            }
            catch (ArgumentException)
            {
                if (value.Contains("-"))
                {
                    value = value.Replace("-", string.Empty);
                    return ParseEnum(type, value);
                }

                throw;
            }
        }
    }
}