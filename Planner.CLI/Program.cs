using JKang.IpcServiceFramework.Client;
using Microsoft.Extensions.DependencyInjection;
using Planner.API.Common;
using Planner.API.Common.Entities;
using Planner.CLI.Actions;
using Planner.CLI.Entities;
using Planner.CLI.Exceptions;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Planner.CLI
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
                Console.WriteLine($"Planner v{versionString}");
                Console.WriteLine("-------------------------");
                Console.WriteLine("Planner command line format:");
                Console.WriteLine("planner <module> <command> [<options>]");
                Console.WriteLine();
                Console.WriteLine("type 'planner <module> --help' to see all avalible commands and options");

                using Stream stream = typeof(Program).Assembly.GetManifestResourceStream("Planner.CLI.Help.Modules.txt");
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
                  .AddNamedPipeIpcClient<IPlannerCommand>("client1", (a, b) => { b.ConnectionTimeout = 10000; b.PipeName = "pipeinternal"; })
                  .BuildServiceProvider();

            var clientFactory = serviceProvider.GetRequiredService<IIpcClientFactory<IPlannerCommand>>();
            BaseCliAction.Proxy = clientFactory.CreateClient("client1");
        }

        private static void HandleCommand(string[] args, IEnumerable<CliActionMetadata> cliActions)
        {
            try
            {
                var action = CliArgumentsUtil.ValidateArgs(ref args, cliActions);
                var cliArgument = new CliArgumentsUtil(args);

                var console = Activator.CreateInstance(action.Method.DeclaringType);
                ActionResponse response;

                if (action.RequestType == null)
                {
                    try
                    {
                        response = (action.Method.Invoke(console, null) as Task<ActionResponse>).Result;
                    }
                    catch (Exception ex)
                    {
                        throw new PlannerServiceException(ex);
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

        private static void HandleException(Exception ex)
        {
            if(ex == null) { return;}

            var finaleException = ex;
            if (ex is AggregateException exception)
            {
                finaleException = exception.InnerExceptions.LastOrDefault();
            }

            if(finaleException == null)
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

        private static ActionResponse InvokeAction(CliActionMetadata action, object console, object param)
        {
            ActionResponse response;
            try
            {
                response = (action.Method.Invoke(console, new[] { param }) as Task<ActionResponse>).Result;
            }
            catch (Exception ex)
            {
                throw new PlannerServiceException(ex);
            }

            return response;
        }

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

        private static void WriteInfo(string message)
        {
            if (string.IsNullOrEmpty(message) == false) { message = message.Trim(); }
            if (message == "[]") { message = null; }
            if (string.IsNullOrEmpty(message)) return;

            AnsiConsole.WriteLine(message);
        }

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

        private static void WriteException(Exception ex)
        {
            AnsiConsole.WriteException(ex,
                ExceptionFormats.ShortenPaths | ExceptionFormats.ShortenTypes |
                ExceptionFormats.ShortenMethods | ExceptionFormats.ShowLinks);
        }
    }
}