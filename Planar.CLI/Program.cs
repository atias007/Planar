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

namespace Planar.CLI
{
    internal class Program
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
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
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

                return true;
            }

            return false;
        }

        private static CliArgumentsUtil HandleCliCommand(string[] args, IEnumerable<CliActionMetadata> cliActions)
        {
            CliArgumentsUtil cliArgument = null;

            try
            {
                var action = CliArgumentsUtil.ValidateArgs(ref args, cliActions);
                cliArgument = new CliArgumentsUtil(args);

                var console = Activator.CreateInstance(action.Method.DeclaringType);
                CliActionResponse response;

                if (action.RequestType == null)
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
                    var param = cliArgument.GetRequest(action.RequestType, action);
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

            if (finaleException == null)
            {
                finaleException = ex;
            }

            if (finaleException.InnerException != null)
            {
                HandleException(finaleException.InnerException);
            }

            if (string.IsNullOrEmpty(finaleException.Message) == false)
            {
                AnsiConsole.MarkupLine($"[red]error: {Markup.Escape(finaleException.Message)}[/]");
            }
        }

        private static void HandleGeneralError(RestResponse response)
        {
            if (response.ErrorMessage.Contains("No connection could be made"))
            {
                AnsiConsole.Markup($"[red]error: no connection could be made to planar deamon ({RestProxy.Host}:{RestProxy.Port})[/]");
            }
            else
            {
                AnsiConsole.Markup("[red]error: general error[/]");
            }

            if (string.IsNullOrEmpty(response.StatusDescription) == false)
            {
                AnsiConsole.Markup($"[red] ({response.StatusDescription})[/]");
            }
        }

        private static void HandleHttpFailResponse(RestResponse response)
        {
            if (HandleHttpNotFoundResponse(response)) { return; }
            if (HandleBadRequestResponse(response)) { return; }

            HandleGeneralError(response);
        }

        private static bool HandleHttpNotFoundResponse(RestResponse response)
        {
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                var message = string.IsNullOrEmpty(response.Content) ?
                    "entity not found" :
                    JsonConvert.DeserializeObject<string>(response.Content);

                AnsiConsole.MarkupLine($"[red]validation error: {message}[/]");
                return true;
            }

            return false;
        }

        private static void InteractiveMode(IEnumerable<CliActionMetadata> cliActions)
        {
            var command = string.Empty;
            Console.Clear();
            WriteInfo();

            const string exit = "exit";
            while (string.Compare(command, exit, true) != 0)
            {
                Console.WriteLine();
                Console.Write($"{RestProxy.Host}:{RestProxy.Port}> ");
                command = Console.ReadLine();
                if (string.Compare(command, exit, true) == 0)
                {
                    break;
                }

                var args = SplitCommandLine(command).ToArray();
                HandleCliCommand(args, cliActions);
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
                .Select(arg => arg.Trim().TrimMatchingQuotes('\"'))
                .Where(arg => !string.IsNullOrEmpty(arg));
        }

        private static void Start(string[] args)
        {
            var cliActions = BaseCliAction.GetAllActions();

            if (args.Length == 0)
            {
                InteractiveMode(cliActions);
            }
            else
            {
                var cliUtil = HandleCliCommand(args, cliActions);
                if (cliUtil != null)
                {
                    if (string.Compare($"{cliUtil.Module}.{cliUtil.Command}", "service.connect", true) == 0)
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

        private static void WriteInfo()
        {
            AnsiConsole.Write(new FigletText("Planar")
                    .LeftAligned()
                    .Color(Color.SteelBlue1));

            var versionString = Assembly.GetEntryAssembly()
                                    ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                                    ?.InformationalVersion
                                    .ToString();

            Console.WriteLine($"planar cli v{versionString}");
            Console.WriteLine("-------------------------");
            Console.WriteLine("usage: planar <module> <command> [<options>]");
            Console.WriteLine();
            Console.WriteLine("use 'planar <module> --help' to see all avalible commands and options");

            using Stream stream = typeof(Program).Assembly.GetManifestResourceStream("Planar.CLI.Help.Modules.txt");
            using StreamReader reader = new(stream);
            string result = reader.ReadToEnd();
            Console.WriteLine(result);
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