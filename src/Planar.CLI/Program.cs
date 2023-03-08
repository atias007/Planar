using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Planar.CLI.Actions;
using Planar.CLI.CliGeneral;
using Planar.CLI.Entities;
using Planar.CLI.Exceptions;
using Planar.CLI.General;
using RestSharp;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace Planar.CLI
{
    internal static class Program
    {
        internal static string Version
        {
            get
            {
                var result = Assembly.GetEntryAssembly()
                                    ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                                    ?.InformationalVersion
                                    .ToString();

                return result ?? string.Empty;
            }
        }

        public static void Main(string[] args)
        {
            Console.CancelKeyPress += Console_CancelKeyPress;

            try
            {
                Start(args);
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
            finally
            {
                Console.CancelKeyPress -= Console_CancelKeyPress;
            }
        }

        private static void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        {
            TokenManager.Cancel();
            e.Cancel = true;
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
                    AnsiConsole.MarkupLine($"[red]  - {(item as JValue)?.Value}[/]");
                }
            }
        }

        private static bool HandleBadRequestResponse(RestResponse response)
        {
            if (response.StatusCode != HttpStatusCode.BadRequest) { return false; }
            if (response.Content == null) { return false; }

            var entity = JsonConvert.DeserializeObject<BadRequestEntity>(response.Content);
            if (entity == null) { return false; }

            var obj = JObject.Parse(response.Content);
            var errors = obj["errors"]?.SelectMany(e => e.ToList()).SelectMany(e => e.ToList());
            if (errors == null) { return false; }

            if (!errors.Any())
            {
                AnsiConsole.MarkupLine(CliFormat.GetValidationErrorMarkup(entity.Detail));
                return true;
            }

            if (errors.Count() == 1)
            {
                var value = errors.First() as JValue;
                var strValue = Convert.ToString(value?.Value);
                AnsiConsole.MarkupLine(CliFormat.GetValidationErrorMarkup(strValue));
                return true;
            }

            AnsiConsole.MarkupLine(CliFormat.GetValidationErrorMarkup(string.Empty));
            DisplayValidationErrors(errors);

            return true;
        }

        private static CliArgumentsUtil? HandleCliCommand(string[]? args, IEnumerable<CliActionMetadata> cliActions)
        {
            if (args == null || !args.Any())
            {
                Console.WriteLine();
                return null;
            }

            CliArgumentsUtil? cliArgument = null;

            try
            {
                var action = CliArgumentsUtil.ValidateArgs(ref args, cliActions);
                if (action == null) { return null; }

                cliArgument = new CliArgumentsUtil(args);

                if (action.Method == null || action.Method.DeclaringType == null) { return null; }

                var console = Activator.CreateInstance(action.Method.DeclaringType);
                if (console == null)
                {
                    return cliArgument;
                }

                CliActionResponse? response;

                if (action.RequestType == null)
                {
                    try
                    {
                        response = InvokeCliAction(action, console, noParameters: true);
                    }
                    catch (Exception ex)
                    {
                        throw new PlanarServiceException(ex);
                    }
                }
                else
                {
                    object? param;
                    using (var scope = new TokenManagerScope())
                    {
                        param = cliArgument.GetRequest(action.RequestType, action, scope.Token);
                    }

                    var itMode = param is IIterative itParam && itParam.Iterative;

                    if (itMode)
                    {
                        var name = $"{action.Method.DeclaringType.Name}.{action.Method.Name}";
                        switch (name)
                        {
                            case "JobCliActions.GetRunningJobs":
                                if (param is CliGetRunningJobsRequest request)
                                {
                                    var scope = new TokenManagerScope();
                                    CliIterativeActions.InvokeGetRunnings(request, scope.Token).Wait();
                                }
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
                HandleExceptionSafe(ex);
            }

            return cliArgument;
        }

        private static void HandleCliResponse(CliActionResponse? response)
        {
            if (response == null) { return; }
            if (response.Response == null) { return; }

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

        private static void HandleExceptionSafe(Exception ex)
        {
            try
            {
                HandleException(ex);
            }
            catch (Exception ex2)
            {
                WriteException(ex2);
            }
        }

        private static void HandleException(Exception ex)
        {
            if (ex == null) { return; }

            if (HasCancelException(ex))
            {
                if (Console.CursorLeft > 0) { AnsiConsole.WriteLine(); }
                AnsiConsole.MarkupLine(CliFormat.GetWarningMarkup("operation was canceled"));
                TokenManager.Reset();
                return;
            }

            var finaleException = ex;
            if (ex is AggregateException exception)
            {
                finaleException = exception.InnerExceptions.LastOrDefault();
            }

            finaleException ??= ex;

            if (finaleException.InnerException != null)
            {
                HandleExceptionSafe(finaleException.InnerException);
            }

            if (string.IsNullOrEmpty(finaleException.Message) == false)
            {
                if (finaleException is CliWarningException)
                {
                    AnsiConsole.MarkupLine(CliFormat.GetWarningMarkup(Markup.Escape(finaleException.Message)));
                }
                else
                {
                    AnsiConsole.MarkupLine(CliFormat.GetErrorMarkup(Markup.Escape(finaleException.Message)));
                }
            }
        }

        private static bool HasCancelException(Exception? ex)
        {
            if (ex == null) { return false; }
            if (ex is OperationCanceledException || ex is TaskCanceledException) { return true; }
            if (ex is AggregateException aggEx)
            {
                foreach (var item in aggEx.InnerExceptions)
                {
                    if (HasCancelException(item)) { return true; }
                }
            }

            return HasCancelException(ex.InnerException);
        }

        private static void HandleGeneralError(RestResponse response)
        {
            if (response.ErrorMessage != null && response.ErrorMessage.Contains("No connection could be made"))
            {
                AnsiConsole.MarkupLine(CliFormat.GetErrorMarkup($"no connection could be made to planar deamon ({RestProxy.Host}:{RestProxy.Port})"));
            }
            else if (response.ErrorMessage != null && response.ErrorMessage.Contains("No such host is known"))
            {
                AnsiConsole.MarkupLine(CliFormat.GetErrorMarkup(response.ErrorMessage.ToLower()));
            }
            else
            {
                AnsiConsole.MarkupLine(CliFormat.GetErrorMarkup("general error"));
            }

            if (!string.IsNullOrEmpty(response.StatusDescription))
            {
                AnsiConsole.MarkupLine($"[red] ({response.StatusDescription})[/]");
            }
        }

        private static bool HandleHealthCheckResponse(RestResponse response)
        {
            if (response.StatusCode == HttpStatusCode.ServiceUnavailable &&
                response.Request != null &&
                response.Content != null &&
                response.Request.Resource.ToLower().Contains("service/healthcheck"))
            {
                var s = JsonConvert.DeserializeObject<string>(response.Content) ?? string.Empty;
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

        private static bool HandleHttpConflictResponse(RestResponse response)
        {
            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                var message = string.IsNullOrEmpty(response.Content) ?
                    "server return conflict status" :
                    JsonConvert.DeserializeObject<string>(response.Content);

                AnsiConsole.MarkupLine(CliFormat.GetConflictErrorMarkup(message));
                return true;
            }

            return false;
        }

        private static void HandleHttpFailResponse(RestResponse response)
        {
            if (HandleHttpNotFoundResponse(response)) { return; }
            if (HandleBadRequestResponse(response)) { return; }
            if (HandleHealthCheckResponse(response)) { return; }
            if (HandleHttpConflictResponse(response)) { return; }

            HandleGeneralError(response);
        }

        private static bool HandleHttpNotFoundResponse(RestResponse response)
        {
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                var message = string.IsNullOrEmpty(response.Content) ?
                    "server return not found status" :
                    JsonConvert.DeserializeObject<string>(response.Content);

                AnsiConsole.MarkupLine(CliFormat.GetValidationErrorMarkup(message));
                return true;
            }

            return false;
        }

        private static void InteractiveMode(IEnumerable<CliActionMetadata> cliActions)
        {
            BaseCliAction.InteractiveMode = true;
            var command = string.Empty;
            Console.Clear();
            CliHelpGenerator.ShowModules();

            const string exit = "exit";
            const string help = "help";
            while (string.Compare(command, exit, true) != 0)
            {
                var color = Login.Current.GetCliMarkupColor();
                AnsiConsole.Markup($"[{color}]{RestProxy.Host.EscapeMarkup()}:{RestProxy.Port}[/]> ");
                command = Console.ReadLine();
                if (string.Compare(command, exit, true) == 0)
                {
                    break;
                }

                if (string.Compare(command, help, true) == 0)
                {
                    CliHelpGenerator.ShowModules();
                }
                else
                {
                    var args = SplitCommandLine(command).ToArray();
                    HandleCliCommand(args, cliActions);
                }
            }
        }

        private static CliActionResponse? InvokeCliAction(CliActionMetadata action, object console, object? param = null, bool noParameters = false)
        {
            if (action.Method == null) { return null; }

            CliActionResponse? response = null;
            try
            {
                using var scope = new TokenManagerScope();
                var args = noParameters ? new object[] { scope.Token } : new[] { param, scope.Token };
                if (action.Method.Invoke(console, args) is Task<CliActionResponse> task)
                {
                    response = task.ConfigureAwait(false).GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                throw new PlanarServiceException(ex);
            }

            return response;
        }

        private static IEnumerable<string> SplitCommandLine(string? commandLine)
        {
            if (string.IsNullOrEmpty(commandLine))
            {
                return new List<string>();
            }

            bool inQuotes = false;

            var split = commandLine.Split(c =>
            {
                if (c == '\"') { inQuotes = !inQuotes; }
                return !inQuotes && c == ' ';
            });

            var final = split
                .Where(arg => !string.IsNullOrEmpty(arg))
                .Select(arg =>
                {
                    return
                    arg == null ?
                    string.Empty :
                    arg.Trim().TrimMatchingQuotes('\"');
                });

            return final;
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

        private static void WriteInfo(string? message)
        {
            if (string.IsNullOrEmpty(message) == false) { message = message.Trim(); }
            if (message == "[]") { message = null; }
            if (string.IsNullOrEmpty(message)) { return; }

            AnsiConsole.WriteLine(message);
        }
    }
}