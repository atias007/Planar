using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Planar.CLI.Actions;
using Planar.CLI.Entities;
using Planar.CLI.Exceptions;
using RestSharp;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Planar.CLI.General;
using System.Text;

namespace Planar.CLI
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                Start(args);
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }

        private static bool HandleBadRequestResponse(RestResponse response)
        {
            if (response.StatusCode != HttpStatusCode.BadRequest) { return false; }

            var entity = JsonConvert.DeserializeObject<BadRequestEntity>(response.Content);

            var obj = JObject.Parse(response.Content);
            var errors = obj["errors"].SelectMany(e => e.ToList()).SelectMany(e => e.ToList());
            if (errors.Any() == false)
            {
                AnsiConsole.MarkupLine($"[red]validation error: {entity.Detail}[/]");
                return true;
            }

            if (errors.Count() == 1)
            {
                var value = errors.First() as JValue;
                AnsiConsole.MarkupLine($"[red]validation error: {value.Value}[/]");
                return true;
            }

            AnsiConsole.MarkupLine("[red]validation error:[/]");
            DisplayValidationErrors(errors);

            return true;
        }

        private static void DisplayValidationErrors(IEnumerable<JToken> errors)
        {
            foreach (JToken item in errors)
            {
                if (item is JArray arr)
                {
                    foreach (JToken subItem in arr)
                    {
                        if (subItem is JValue jvalue)
                        {
                            AnsiConsole.MarkupLine($"[red]  - {jvalue.Value}[/]");
                        }
                    }
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]  - {(item as JValue).Value}[/]");
                }
            }
        }

        private static CliArgumentsUtil HandleCliCommand(string[] args, IEnumerable<CliActionMetadata> cliActions)
        {
            if (!args.Any())
            {
                return null;
            }

            CliArgumentsUtil cliArgument = null;

            try
            {
                var action = CliArgumentsUtil.ValidateArgs(ref args, cliActions);
                cliArgument = new CliArgumentsUtil(args);

                var console = Activator.CreateInstance(action.Method.DeclaringType);
                var requestType = action.GetRequestType();
                CliActionResponse response;

                if (requestType == null)
                {
                    try
                    {
                        response = (action.Method.Invoke(console, null) as Task<CliActionResponse>)?.Result;
                    }
                    catch (Exception ex)
                    {
                        throw new PlanarServiceException(ex);
                    }
                }
                else
                {
                    var param = cliArgument.GetRequest(requestType, action);
                    var itMode = param is IIterative itParam && itParam.Iterative;

                    if (itMode)
                    {
                        var name = $"{action.Method.DeclaringType.Name}.{action.Method.Name}";
                        switch (name)
                        {
                            case "JobCliActions.GetRunningJobs":
                                CliIterativeActions.InvokeGetRunnings((CliGetRunningJobsRequest)param).Wait();
                                break;

                            default:
                                break;
                        }

                        response = null;
                    }
                    else
                    {
                        response = InvokeCliAction(action, console, param);
                    }
                }

                HandleCliResponse(response);
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }

            return cliArgument;
        }

        private static void HandleCliResponse(CliActionResponse response)
        {
            if (response == null) return;
            if (response.Response == null) return;

            if (response.Response.IsSuccessful)
            {
                if (response.Tables != null)
                {
                    response.Tables.ForEach(t => AnsiConsole.Write(t));
                }
                else
                {
                    WriteInfo(response.Message);
                }
            }
            else
            {
                HandleHttpFailResponse(response.Response);
            }
        }

        private static void HandleException(Exception ex)
        {
            if (ex == null) { return; }

            var finaleException = ex;
            if (ex is AggregateException exception)
            {
                finaleException = exception.InnerExceptions.LastOrDefault();
            }

            finaleException ??= ex;

            if (finaleException.InnerException != null)
            {
                HandleException(finaleException.InnerException);
            }

            if (string.IsNullOrEmpty(finaleException.Message) == false)
            {
                if (finaleException is CliWarningException)
                {
                    AnsiConsole.MarkupLine($"{CliTableFormat.WarningColor}warning: {Markup.Escape(finaleException.Message)}[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine($"{CliTableFormat.ErrorColor}error: {Markup.Escape(finaleException.Message)}[/]");
                }
            }
        }

        private static void HandleGeneralError(RestResponse response)
        {
            if (response.ErrorMessage != null && response.ErrorMessage.Contains("No connection could be made"))
            {
                AnsiConsole.MarkupLine($"[red]error: no connection could be made to planar deamon ({RestProxy.Host}:{RestProxy.Port})[/]");
            }
            else if (response.ErrorMessage != null && response.ErrorMessage.Contains("No such host is known"))
            {
                AnsiConsole.MarkupLine($"[red]{response.ErrorMessage.ToLower()}[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[red]error: general error[/]");
            }

            if (string.IsNullOrEmpty(response.StatusDescription) == false)
            {
                AnsiConsole.MarkupLine($"[red] ({response.StatusDescription})[/]");
            }
        }

        private static void HandleHttpFailResponse(RestResponse response)
        {
            if (HandleHttpNotFoundResponse(response)) { return; }
            if (HandleBadRequestResponse(response)) { return; }
            if (HandleHealthCheckResponse(response)) { return; }
            if (HandleHttpConflictResponse(response)) { return; }

            HandleGeneralError(response);
        }

        private static bool HandleHealthCheckResponse(RestResponse response)
        {
            if (response.StatusCode == HttpStatusCode.ServiceUnavailable &&
                response.Request.Resource.ToLower().Contains("service/healthcheck"))
            {
                var s = JsonConvert.DeserializeObject<string>(response.Content);
                var lines = s.Split("\r", StringSplitOptions.TrimEntries);
                foreach (var item in lines)
                {
                    if (item.Contains("unhealthy"))
                    {
                        AnsiConsole.MarkupLine($"[red]{item.EscapeMarkup()}[/]");
                    }
                    else
                    {
                        AnsiConsole.WriteLine(item);
                    }
                }

                return true;
            }

            return false;
        }

        private static bool HandleHttpNotFoundResponse(RestResponse response)
        {
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                var message = string.IsNullOrEmpty(response.Content) ?
                    "server return not found status" :
                    JsonConvert.DeserializeObject<string>(response.Content);

                AnsiConsole.MarkupLine($"[red]validation error: {message}[/]");
                return true;
            }

            return false;
        }

        private static bool HandleHttpConflictResponse(RestResponse response)
        {
            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                var message = string.IsNullOrEmpty(response.Content) ?
                    "server return conflict status" :
                    JsonConvert.DeserializeObject<string>(response.Content);

                AnsiConsole.MarkupLine($"[red]conflict error: {message}[/]");
                return true;
            }

            return false;
        }

        private static void InteractiveMode(IEnumerable<CliActionMetadata> cliActions)
        {
            BaseCliAction.InteractiveMode = true;
            var command = string.Empty;
            Console.Clear();
            WriteInfo();

            const string exit = "exit";
            const string help = "help";
            while (string.Compare(command, exit, true) != 0)
            {
                Console.Write($"{RestProxy.Host}:{RestProxy.Port}> ");
                command = Console.ReadLine();
                if (string.Compare(command, exit, true) == 0)
                {
                    break;
                }

                if (string.Compare(command, help, true) == 0)
                {
                    WriteInfo();
                }
                else
                {
                    var args = SplitCommandLine(command).ToArray();
                    HandleCliCommand(args, cliActions);
                }
            }
        }

        private static CliActionResponse InvokeCliAction(CliActionMetadata action, object console, object param)
        {
            CliActionResponse response;
            try
            {
                response = (action.Method.Invoke(console, new[] { param }) as Task<CliActionResponse>).Result;
            }
            catch (Exception ex)
            {
                throw new PlanarServiceException(ex);
            }

            return response;
        }

        private static IEnumerable<string> SplitCommandLine(string commandLine)
        {
            bool inQuotes = false;

            return commandLine.Split(c =>
            {
                if (c == '\"')
                    inQuotes = !inQuotes;

                return !inQuotes && c == ' ';
            })
                .Select(arg => arg?.Trim().TrimMatchingQuotes('\"'))
                .Where(arg => !string.IsNullOrEmpty(arg));
        }

        private static void Start(string[] args)
        {
            var cliActions = BaseCliAction.GetAllActions();
            ServiceCliActions.InitializeLogin();

            if (args.Length == 0)
            {
                InteractiveMode(cliActions);
            }
            else
            {
                var cliUtil = HandleCliCommand(args, cliActions);
                if (cliUtil != null)
                {
                    var command = $"{cliUtil.Module}.{cliUtil.Command}";
                    if (string.Equals(command, "service.login", StringComparison.OrdinalIgnoreCase) && cliUtil.HasIterativeArgument)
                    {
                        InteractiveMode(cliActions);
                    }
                }
            }
        }

        private static void WriteException(Exception ex)
        {
            AnsiConsole.WriteException(ex,
                ExceptionFormats.ShortenPaths | ExceptionFormats.ShortenTypes |
                ExceptionFormats.ShortenMethods | ExceptionFormats.ShowLinks);
        }

        internal static string Version
        {
            get
            {
                var result = Assembly.GetEntryAssembly()
                                    ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                                    ?.InformationalVersion
                                    .ToString();

                return result;
            }
        }

        private static void WriteInfo()
        {
            AnsiConsole.Write(new FigletText("Planar")
                    .LeftJustified()
                    .Color(Color.SteelBlue1));

            Console.WriteLine(GetHelpHeader());
            Console.WriteLine("usage: planar-cli <module> <command> [<options>]");
            Console.WriteLine();
            Console.WriteLine("use 'planar-cli <module> --help' to see all avalible commands and options");

            using Stream stream = typeof(Program).Assembly.GetManifestResourceStream("Planar.CLI.Help.Modules.txt");
            using StreamReader reader = new(stream);
            string result = reader.ReadToEnd();
            Console.WriteLine(result);
            Console.WriteLine();
        }

        public static string GetHelpHeader()
        {
            const string seperator = "-------------------------";
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine(seperator);
            sb.AppendLine($"planar cli v{Version}");
            sb.AppendLine(seperator);
            return sb.ToString();
        }

        private static void WriteInfo(string message)
        {
            if (string.IsNullOrEmpty(message) == false) { message = message.Trim(); }
            if (message == "[]") { message = null; }
            if (string.IsNullOrEmpty(message)) return;

            AnsiConsole.WriteLine(message);
        }
    }
}