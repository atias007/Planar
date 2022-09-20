using Planar.CLI.Actions;
using Planar.CLI.Exceptions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Planar.CLI
{
    public class CliArgumentsUtil
    {
        public CliArgumentsUtil(string[] args)
        {
            Module = args[0];
            Command = args[1];

            CliArgument current = null;

            for (int i = 2; i < args.Length; i++)
            {
                var item = args[i];
                if (current == null)
                {
                    current = new CliArgument { Key = item };
                }
                else
                {
                    if (current.Key.StartsWith("-") && item.StartsWith("-") == false)
                    {
                        current.Value = item;
                        CliArguments.Add(current);
                        current = null;
                    }
                    else
                    {
                        CliArguments.Add(current);
                        current = new CliArgument { Key = item };
                    }
                }
            }

            if (current != null) CliArguments.Add(current);
        }

        public static CliActionMetadata ValidateArgs(ref string[] args, IEnumerable<CliActionMetadata> cliActionsMetadata)
        {
            var list = args.ToList();



            if (list[0].ToLower() == "ls") { list.Insert(0, "job"); }
            if (list[0].ToLower() == "connect") { list.Insert(0, "service"); }
            if (list[0].ToLower() == "cls") { list.Insert(0, "inner"); }

            if (cliActionsMetadata.Any(a => a.Module.ToLower() == list[0].ToLower()) == false)
            {
                throw new ValidationException($"module '{list[0]}' is not supported");
            }

            if (list.Count == 1)
            {
                throw new ValidationException($"missing command for module '{list[0]}'\r\n. command line format is 'Planar <module> <command> [<options>]'");
            }

            if (cliActionsMetadata.Any(a => a.Command.Any(c => c?.ToLower() == list[1].ToLower())) == false)
            {
                throw new ValidationException($"module '{list[0]}' does not support command '{list[1]}'");
            }

            var action = cliActionsMetadata.FirstOrDefault(a => a.Command.Any(c => c?.ToLower() == list[1].ToLower() && a.Module == list[0].ToLower()));
            if (action == null)
            {
                throw new ValidationException($"module '{list[0]}' does not support command '{list[1]}'");
            }

            args = list.ToArray();
            return action;
        }

        public string Module { get; set; }

        public string Command { get; set; }

        public List<CliArgument> CliArguments { get; set; } = new List<CliArgument>();

        public object GetRequest(Type type, CliActionMetadata action)
        {
            var result = Activator.CreateInstance(type);
            var props = action.GetRequestPropertiesInfo();

            var defaultProps = props.Where(p => p.Default);
            var startDefaultOrder = defaultProps.Any() ? defaultProps.Min(p => p.DefaultOrder) : -1;

            foreach (var a in CliArguments)
            {
                RequestPropertyInfo matchProp = null;
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
                    matchProp = props
                        .Where(p => p.Default && (p.DefaultOrder == startDefaultOrder))
                        .FirstOrDefault();

                    startDefaultOrder++;

                    a.Value = a.Key;
                }

                if (matchProp == null)
                {
                    throw new ValidationException($"Argument '{a.Key}' is not supported with command '{action.Command.FirstOrDefault()}' at module '{action.Module}'");
                }

                if (a.Key.StartsWith("-") && string.IsNullOrEmpty(a.Value))
                {
                    a.Value = true.ToString();
                }

                FillLastJobOrTriggerId(matchProp, a);
                FillJobId(matchProp, a).Wait();
                SetValue(matchProp.PropertyInfo, result, a.Value);
                if (!string.IsNullOrEmpty(a.Value))
                {
                    matchProp.ValueSupplied = true;
                }
            }

            FindMissingRequiredProperties(props);

            return result;
        }

        private static async Task FillJobId(RequestPropertyInfo prop, CliArgument arg)
        {
            if (prop.JobOrTriggerKey)
            {
                if (arg.Value == "?")
                {
                    var jobId = await JobCliActions.ChooseJob();
                    arg.Value = jobId;
                    Util.LastJobOrTriggerId = jobId;
                }
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
            var p = props.FirstOrDefault(v => v.Required && v.ValueSupplied == false);
            if (p != null)
            {
                var message =
                    string.IsNullOrEmpty(p.RequiredMissingMessage) ?
                    $"property {p.Name} is required" :
                    p.RequiredMissingMessage;

                throw new CliException(message);
            }
        }

        private static void SetValue(PropertyInfo prop, object instance, string value)
        {
            try
            {
                object objValue = value;
                if (prop.PropertyType.BaseType == typeof(Enum))
                {
                    objValue = Enum.Parse(prop.PropertyType, value);
                }
                prop.SetValue(instance, Convert.ChangeType(objValue, prop.PropertyType, CultureInfo.CurrentCulture));
            }
            catch (Exception)
            {
                throw new CliException($"Value '{value}' has wrong format for type '{prop.PropertyType.Name}' of property {prop.Name}");
            }
        }
    }
}