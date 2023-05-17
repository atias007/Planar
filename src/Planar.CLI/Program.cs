using Dumpify;
using Dumpify.Config;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Planar.CLI.Actions;
using Planar.CLI.CliGeneral;
using Planar.CLI.DataProtect;
using Planar.CLI.Entities;
using Planar.CLI.Exceptions;
using Planar.CLI.General;
using Planar.CLI.Proxy;
using RestSharp;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.CLI
{
    internal static class Program
    {
        private static readonly TimeSpan _timerSpan = TimeSpan.FromMinutes(20);
        private static Timer? _timer;

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
            Console.OutputEncoding = Encoding.UTF8;
            Console.CancelKeyPress += Console_CancelKeyPress;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.Title = "Planar: Command Line Interface";
            }

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

        private static void ResetTimer()
        {
            _timer?.Change(_timerSpan, _timerSpan);
        }

        private static void OnTimerAction(object? state)
        {
            if (string.IsNullOrEmpty(LoginProxy.Token)) { return; }
            InnerCliActions.Clear().Wait();
            var markup = CliFormat.GetWarningMarkup($"automaticaly log out after period of {_timerSpan.Minutes} minutes without any operation");
            AnsiConsole.MarkupLine(markup);
            AnsiConsole.WriteLine("press [enter] to continue");
            ServiceCliActions.Logout().Wait();
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
                MarkupCliLine(CliFormat.GetValidationErrorMarkup(entity.Detail));
                return true;
            }

            if (errors.Count() == 1)
            {
                var value = errors.First() as JValue;
                var strValue = Convert.ToString(value?.Value);
                MarkupCliLine(CliFormat.GetValidationErrorMarkup(strValue));
                return true;
            }

            MarkupCliLine(CliFormat.GetValidationErrorMarkup(string.Empty));
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
                        param = cliArgument.GetRequest(action, TokenManagerScope.Token);
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
                                    using var scope = new TokenManagerScope();
                                    CliIterativeActions.InvokeGetRunnings(request, TokenManagerScope.Token).Wait();
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

                HandleCliResponse(response).Wait();
            }
            catch (Exception ex)
            {
                HandleExceptionSafe(ex);
            }

            return cliArgument;
        }

        private static async Task HandleCliResponse(CliActionResponse? response)
        {
            if (response == null) { return; }
            if (response.Response == null) { return; }

            if (response.Response.IsSuccessful)
            {
                if (response.Tables != null)
                {
                    response.Tables.ForEach(t => AnsiConsole.Write(t));
                }
                else if (response.DumpObject != null)
                {
                    DumpConfig.Default.TableConfig.ShowTableHeaders = false;
                    DumpConfig.Default.ShowHeaders = false;
                    DumpConfig.Default.TypeNamingConfig.ShowTypeNames = false;

                    response.DumpObject.Dump();
                }
                else
                {
                    await WriteInfo(response);
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
            if (HandleCancelException(ex)) { return; }

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

            if (!string.IsNullOrEmpty(finaleException.Message))
            {
                if (finaleException is CliWarningException)
                {
                    AnsiConsole.MarkupLine(CliFormat.GetWarningMarkup(finaleException.Message));
                }
                else
                {
                    MarkupCliLine(CliFormat.GetErrorMarkup(finaleException.Message));
                }
            }
        }

        private static bool HandleCancelException(Exception? ex)
        {
            if (HasCancelException(ex))
            {
                if (Console.CursorLeft > 0) { AnsiConsole.WriteLine(); }
                AnsiConsole.MarkupLine(CliFormat.GetWarningMarkup("operation was canceled"));
                TokenManager.Reset();
                return true;
            }

            return false;
        }

        private static bool HasCancelException(Exception? ex)
        {
            if (ex == null) { return false; }
            if (ex is OperationCanceledException || ex is TaskCanceledException) { return true; }
            if (ex is AggregateException aggEx)
            {
                var hasCancel = aggEx.InnerExceptions.Any(item => HasCancelException(item));
                if (hasCancel) { return true; }
            }

            return HasCancelException(ex.InnerException);
        }

        private static void HandleGeneralError(RestResponse response)
        {
            if (HandleCancelException(response.ErrorException)) { return; }

            if (response.ErrorMessage != null && response.ErrorMessage.Contains("No connection could be made"))
            {
                var message = response.ErrorMessage.Replace("because the target machine actively refused it.", "to planar deamon");
                MarkupCliLine(CliFormat.GetErrorMarkup(message.ToLower()));
            }
            else if (response.ErrorMessage != null && response.ErrorMessage.Contains("No such host is known"))
            {
                MarkupCliLine(CliFormat.GetErrorMarkup(response.ErrorMessage.ToLower()));
            }
            else
            {
                MarkupCliLine(CliFormat.GetErrorMarkup("general error"));
            }

            if (!string.IsNullOrEmpty(response.StatusDescription))
            {
                MarkupCliLine($"[red] ({response.StatusDescription})[/]");
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

                MarkupCliLine(CliFormat.GetConflictErrorMarkup(message));
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
            if (HandleHttpUnauthorizedResponse(response)) { return; }

            HandleGeneralError(response);
        }

        private static bool HandleHttpUnauthorizedResponse(RestResponse response)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                MarkupCliLine(CliFormat.GetUnauthorizedErrorMarkup());
                return true;
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                MarkupCliLine(CliFormat.GetForbiddenErrorMarkup());
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

                MarkupCliLine(CliFormat.GetValidationErrorMarkup(message));
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
            _timer = new Timer(OnTimerAction, null, _timerSpan, _timerSpan);

            const string exit = "exit";
            while (string.Compare(command, exit, true) != 0)
            {
                var color = ConnectUtil.Current.GetCliMarkupColor();
                AnsiConsole.Markup($"[{color}]{RestProxy.Host.EscapeMarkup()}:{RestProxy.Port}[/]> ");
                command = Console.ReadLine();
                ResetTimer();

                if (string.Compare(command, exit, true) == 0)
                {
                    break;
                }

                var args = SplitCommandLine(command).ToArray();
                HandleCliCommand(args, cliActions);
            }
        }

        private static CliActionResponse? InvokeCliAction(CliActionMetadata action, object console, object? param = null, bool noParameters = false)
        {
            if (action.Method == null) { return null; }

            CliActionResponse? response = null;
            try
            {
                using var scope = new TokenManagerScope();
                var args = noParameters ? new object[] { TokenManagerScope.Token } : new[] { param, TokenManagerScope.Token };
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

#if DEBUG
            //// var md = CliHelpGenerator.GetHelpMD(cliActions);
#endif

            ServiceCliActions.InitializeLogin().Wait();

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

        private static async Task WriteInfo(CliActionResponse response)
        {
            var message = response.Message;
            if (!string.IsNullOrEmpty(message)) { message = message.Trim(); }
            if (message == "[]") { message = null; }
            if (string.IsNullOrEmpty(message)) { return; }

            if (response.OutputFilename == null)
            {
                AnsiConsole.WriteLine(message);
            }
            else
            {
                var filename = response.OutputFilename ?? string.Empty;
                if (!filename.Contains('.')) { filename = $"{filename}.txt"; }
                await SaveData(message, filename);
                AnsiConsole.WriteLine($"file '{new FileInfo(filename).FullName}' created");
            }
        }

        private static async Task SaveData(string? content, string filename)
        {
            if (filename == null) { return; }
            await File.AppendAllTextAsync(filename, content);
        }

        private static void MarkupCliLine(string message)
        {
            if (Console.CursorLeft != 0)
            {
                Console.WriteLine();
            }

            AnsiConsole.MarkupLine(message);
        }
    }
}