using JKang.IpcServiceFramework.Client;
using Microsoft.Extensions.DependencyInjection;
using Planar.API.Common;
using Planar.API.Common.Entities;
using Planar.CLI.Actions;
using Planar.CLI.Entities;
using Planar.CLI.Exceptions;
using RestSharp;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Planar.CLI
{
    internal class Program
    {
        private static void Main(string[] args)
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

        private static void Start(string[] args)
        {
            var cliActions = BaseCliAction.GetAllActions();

            if (args.Length == 0)
            {
                var versionString = Assembly.GetEntryAssembly()
                                        ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                                        ?.InformationalVersion
                                        .ToString();

                Console.WriteLine("-------------------------");
                Console.WriteLine($"Planar v{versionString}");
                Console.WriteLine("-------------------------");
                Console.WriteLine("Planar command line format:");
                Console.WriteLine("Planar <module> <command> [<options>]");
                Console.WriteLine();
                Console.WriteLine("type 'Planar <module> --help' to see all avalible commands and options");

                using Stream stream = typeof(Program).Assembly.GetManifestResourceStream("Planar.CLI.Help.Modules.txt");
                using StreamReader reader = new(stream);
                string result = reader.ReadToEnd();
                Console.WriteLine(result);
            }
            else
            {
                LoadNetPipeProxy();
                HandleCommand(args, cliActions);
            }
        }

        private static void LoadNetPipeProxy()
        {
            var serviceProvider = new ServiceCollection()
                  .AddNamedPipeIpcClient<IPlanarCommand>("client1", (a, b) => { b.ConnectionTimeout = 10000; b.PipeName = "pipeinternal"; })
                  .BuildServiceProvider();

            var clientFactory = serviceProvider.GetRequiredService<IIpcClientFactory<IPlanarCommand>>();
            BaseCliAction.Proxy = clientFactory.CreateClient("client1");
        }

        // TODO: remove method && ActionResponse class
        private static void HandleCommand(string[] args, IEnumerable<CliActionMetadata> cliActions)
        {
            try
            {
                var action = CliArgumentsUtil.ValidateArgs(ref args, cliActions);
                var cliArgument = new CliArgumentsUtil(args);

                if (cliArgument.Module == "service" || cliArgument.Module == "group" || cliArgument.Module == "history" || cliArgument.Module == "job" || cliArgument.Module == "monitor")
                {
                    HandleCliCommand(args, cliActions);
                    return;
                }

                var console = Activator.CreateInstance(action.Method.DeclaringType);
                ActionResponse response;

                if (action.RequestType == null)
                {
                    try
                    {
                        response = (action.Method.Invoke(console, null) as Task<ActionResponse>)?.Result;
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
                        response = InvokeAction(action, console, param);
                    }
                }

                HandleResponse(response);
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private static void HandleCliCommand(string[] args, IEnumerable<CliActionMetadata> cliActions)
        {
            try
            {
                var action = CliArgumentsUtil.ValidateArgs(ref args, cliActions);
                var cliArgument = new CliArgumentsUtil(args);

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

        // TODO: to be deleted
        private static ActionResponse InvokeAction(CliActionMetadata action, object console, object param)
        {
            ActionResponse response;
            try
            {
                var result = action.Method.Invoke(console, new[] { param });
                response = (result as Task<ActionResponse>).Result;
            }
            catch (Exception ex)
            {
                throw new PlanarServiceException(ex);
            }

            return response;
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

        // TODO: to be deleted
        private static void HandleResponse(ActionResponse response)
        {
            if (response == null) return;

            if (response.Tables != null)
            {
                response.Tables.ForEach(t => AnsiConsole.Write(t));
            }
            else if (!(string.IsNullOrEmpty(response.Message) == false && response.Response.Success))
            {
                WriteResult(response.Response);
            }
            else if (response.Response.Success) //-V3022
            {
                WriteInfo(response.Message);
            }
        }

        private static void HandleCliResponse(CliActionResponse response)
        {
            if (response == null) return;

            if (response.Tables != null)
            {
                response.Tables.ForEach(t => AnsiConsole.Write(t));
            }
            else if (!(string.IsNullOrEmpty(response.Message) == false && response.Response.IsSuccessful))
            {
                WriteCliResult(response.Response);
            }
            else if (response.Response.IsSuccessful) //-V3022
            {
                WriteInfo(response.Message);
            }
        }

        private static void WriteInfo(string message)
        {
            if (string.IsNullOrEmpty(message) == false) { message = message.Trim(); }
            if (message == "[]") { message = null; }
            if (string.IsNullOrEmpty(message)) return;

            AnsiConsole.WriteLine(message);
        }

        // TODO: to be deleted
        private static void WriteResult(BaseResponse result)
        {
            if (result != null)
            {
                if (result.Success == false)
                {
                    if (result.ErrorCode > 0)
                    {
                        AnsiConsole.Markup($"[red]error: ({result.ErrorCode}) {result.ErrorDescription}[/]");
                    }
                    else
                    {
                        AnsiConsole.Markup($"[red]error: {result.ErrorDescription}[/]");
                    }
                }
            }
        }

        private static void WriteCliResult(RestResponse result)
        {
            if (result != null)
            {
                if (result.IsSuccessful == false)
                {
                    if (string.IsNullOrEmpty(result.ErrorMessage) == false)
                    {
                        AnsiConsole.Markup($"[red]error: {result.ErrorMessage}[/]");
                    }
                    else if (string.IsNullOrEmpty(result.ErrorException?.Message) == false)
                    {
                        AnsiConsole.Markup($"[red]error: {result.ErrorException?.Message}[/]");
                    }
                    else
                    {
                        AnsiConsole.Markup($"[red]error: general error ({result.StatusDescription})[/]");
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
    }
}