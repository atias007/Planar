using Newtonsoft.Json.Linq;
using Planar.CLI.Attributes;
using RestSharp;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace Planar.CLI.Actions
{
    public class BaseCliAction
    {
        protected const string JobFileName = "JobFile.yml";

        protected static async Task<CliActionResponse> Execute(RestRequest request)
        {
            var result = await RestProxy.Invoke(request);
            return new CliActionResponse(result);
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
            if (result.IsSuccessful)
            {
                var table = tableFunc.Invoke(result.Data);
                return new CliActionResponse(result, table);
            }

            return new CliActionResponse(result);
        }

        private static object ConvertJTokenToObject(JToken token)
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

            throw new InvalidOperationException("Unexpected token: " + token);
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

            var help = typeof(BaseCliAction<T>).GetMethod(nameof(ShowHelp));
            allActions.Add(help);

            // TODO: check for moduleAttribute == null;
            foreach (var act in allActions)
            {
                var actionAttributes = act.GetCustomAttributes<ActionAttribute>();

                // TODO: validate attributes (invalid name...)
                if (actionAttributes != null)
                {
                    var item = new CliActionMetadata
                    {
                        Module = moduleAttribute?.Name?.ToLower(),
                        Method = act,
                        Command = actionAttributes.Select(a => a.Name).Distinct().ToList()
                    };

                    result.Add(item);
                }
            }

            return result;
        }

        [Action("help")]
        [Action("--help")]
        [Action("-h")]
        public static async Task<CliActionResponse> ShowHelp()
        {
            var name = typeof(T).Name.Replace("CliActions", string.Empty);
            var help = GetHelpResource(name);
            var response = new RestResponse { StatusCode = HttpStatusCode.OK, ResponseStatus = ResponseStatus.Completed, IsSuccessStatusCode = true };
            var result = new CliActionResponse(response, help);
            return await Task.FromResult(result);
        }

        private static string GetHelpResource(string name)
        {
            using Stream stream = typeof(Program).Assembly.GetManifestResourceStream($"Planar.CLI.Help.{name}.txt");
            using StreamReader reader = new(stream);
            var result = reader.ReadToEnd();
            return result;
        }

        protected static TRequest CollectDataFromCli<TRequest>()
            where TRequest : class, new()
        {
            AnsiConsole.MarkupLine("[underline]Enter values for the following properties:[/]");

            var prm = Activator.CreateInstance<TRequest>();
            var properties = typeof(TRequest).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                var type = prop.PropertyType.Name;

                switch (type)
                {
                    case "Int32":
                        var value1 = AnsiConsole.Ask<int>($"[turquoise2]  > {prop.Name}[/]: ");
                        prop.SetValue(prm, value1);
                        break;

                    case "String":
                    default:
                        var value2 = AnsiConsole.Prompt(new TextPrompt<string>($"[turquoise2]  > {prop.Name}[/]: ").AllowEmpty());
                        if (string.IsNullOrEmpty(value2)) { value2 = null; }
                        prop.SetValue(prm, value2);
                        break;
                }
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[underline]Result:[/]");

            return prm;
        }
    }
}