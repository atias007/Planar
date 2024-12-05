using AsciiChart.Sharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Planar.API.Common.Entities;
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.CLI;

internal static class Program
{
    private static readonly TimeSpan _timerSpan = TimeSpan.FromMinutes(20);
    private static Timer? _timer;

    internal static string Version
    {
        get
        {
            var result = Assembly.GetEntryAssembly()
                                ?.GetCustomAttribute<AssemblyFileVersionAttribute>()
                                ?.Version;

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
                        var message = Convert.ToString(jvalue.Value)?.EscapeMarkup();
                        AnsiConsole.MarkupLine($"[red]  - {message}[/]");
                    }
                }
            }
            else
            {
                var message = Convert.ToString((item as JValue)?.Value)?.EscapeMarkup();
                AnsiConsole.MarkupLine($"[red]  - {message}[/]");
            }
        }
    }

    private static string? SafeFromBase64ToiString(string base64)
    {
        try
        {
            var bytes = Convert.FromBase64String(base64);
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return null;
        }
    }

    private static string? GetCliMessage(RestResponse response)
    {
        var cliHeader = response.Headers?.FirstOrDefault(h => h.Name == Consts.CliMessageHeaderName);
        var result = Convert.ToString(cliHeader?.Value);
        if (!string.IsNullOrWhiteSpace(result))
        {
            result = SafeFromBase64ToiString(result);
        }

        return result;
    }

    private static string? GetCliSuggestion(RestResponse response)
    {
        var cliHeader = response.Headers?.FirstOrDefault(h => h.Name == Consts.CliSuggestionHeaderName);
        var result = Convert.ToString(cliHeader?.Value);
        if (!string.IsNullOrWhiteSpace(result))
        {
            result = SafeFromBase64ToiString(result);
        }

        return result;
    }

    private static bool HandleHeaderMessage(RestResponse response)
    {
        var cliHeaderMessage = GetCliMessage(response);
        if (string.IsNullOrWhiteSpace(cliHeaderMessage)) { return false; }
        MarkupCliLine(CliFormat.GetValidationErrorMarkup(cliHeaderMessage));

        var cliHeaderSuggestion = GetCliSuggestion(response);
        if (string.IsNullOrWhiteSpace(cliHeaderSuggestion)) { return false; }
        MarkupCliLine(CliFormat.GetSuggestionMarkup(cliHeaderSuggestion));

        return true;
    }

    private static bool HandleBadRequestResponse(RestResponse response)
    {
        if (response.StatusCode != HttpStatusCode.BadRequest) { return false; }
        if (HandleHeaderMessage(response)) { return true; }
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
        if (args == null || args.Length == 0)
        {
            Console.WriteLine();
            return null;
        }

        CliArgumentsUtil? cliArgument = null;

        try
        {
            var action = CliArgumentsUtil.ValidateArgs(ref args, cliActions);
            if (action == null) { return null; }

            cliArgument = new CliArgumentsUtil(args)
            {
                RequestType = action.RequestType
            };

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

                    // paging request
                    if (param is IPagingRequest pagingRequest)
                    {
                        var paging = response?.GetPagingResponse();
                        if (paging != null && !paging.IsLastPage)
                        {
                            do
                            {
                                HandleCliResponse(response, cliArgument.OutputFilename);
                                pagingRequest.SetPagingDefaults();
                                var assert = AssertPage(pagingRequest);
                                if (assert == AssertMembers.Next) { pagingRequest.PageNumber++; }
                                else if (assert == AssertMembers.Prev) { pagingRequest.PageNumber--; }
                                else { return cliArgument; }

                                response = InvokeCliAction(action, console, param);
                                paging = response?.GetPagingResponse();
                            } while (paging?.IsLastPage == false);
                        }
                    }
                }
            }

            HandleCliResponse(response, cliArgument.OutputFilename);
        }
        catch (Exception ex)
        {
            if (ex is CliException cliEx)
            {
                HandleExceptionSafe(cliEx, cliArgument?.OutputFilename);
            }
            else if (ex.InnerException is CliException cliInnerEx)
            {
                HandleExceptionSafe(cliInnerEx, cliArgument?.OutputFilename);
            }
            else
            {
                HandleExceptionSafe(ex);
            }
        }

        return cliArgument;
    }

    private static AssertMembers AssertPage(IPagingRequest request)
    {
        const string nextMarkup = $" [turquoise2][[Page Down]][/] for next page";
        var prevMarkup = $", [{CliFormat.WarningColor}][[Page Up]][/] for previous";
        var cancelMarkup = $", [{CliFormat.ErrorColor}][[Enter]][/] to cancel paging ";

        if (request.PageNumber == 1)
        {
            AnsiConsole.Markup($"{nextMarkup}{cancelMarkup}");
        }
        else
        {
            AnsiConsole.Markup($"{nextMarkup}{prevMarkup}{cancelMarkup}");
        }

        AssertMembers result;
        while (true)
        {
            var key = Console.ReadKey(true);
            var modifier = key.Modifiers == ConsoleModifiers.Control || key.Modifiers == ConsoleModifiers.Shift || key.Modifiers == ConsoleModifiers.Shift;
            var cancel = !modifier && key.Key == ConsoleKey.Enter;

            if (!modifier && key.Key == ConsoleKey.PageUp && request.PageNumber > 1)
            {
                result = AssertMembers.Prev;
                break;
            }

            if (!modifier && key.Key == ConsoleKey.PageDown)
            {
                result = AssertMembers.Next;
                break;
            }

            if (cancel)
            {
                result = AssertMembers.Exit;
                break;
            }
        }

        ClearCurrentConsoleLine();
        return result;
    }

    private static void ClearCurrentConsoleLine()
    {
        int currentLineCursor = Console.CursorTop;
        Console.SetCursorPosition(0, Console.CursorTop);
        Console.Write(new string(' ', Console.WindowWidth));
        Console.SetCursorPosition(0, currentLineCursor);
    }

    private enum AssertMembers
    {
        Next,
        Prev,
        Exit
    }

    private static void HandleCliResponse(CliActionResponse? response, string? outputfilename)
    {
        if (response == null) { return; }
        if (response.Response == null) { return; }

        if (!response.Response.IsSuccessful)
        {
            HandleHttpFailResponse(response.Response);
            return;
        }

        if (string.IsNullOrEmpty(outputfilename))
        {
            WriteCliResponse(AnsiConsole.Console, response);
        }
        else
        {
            var isHtml = IsHtmlFilename(outputfilename);
            using var recorder = AnsiConsole.Console.CreateRecorder();
            WriteCliResponse(recorder, response);

            var output =
                isHtml ?
                recorder.ExportHtml() :
                recorder.ExportText();

            output += Environment.NewLine;

            SafeCreateFile(outputfilename, output);
        }
    }

    private static void SafeCreateFile(string outputfilename, string output)
    {
        try
        {
            File.WriteAllText(outputfilename, output, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            throw new CliException($"fail to write content to file '{outputfilename}'. error message: {ex.Message}");
        }
    }

    private static bool IsHtmlFilename(string filename)
    {
        var fi = new FileInfo(filename);
        var ext = fi.Extension.ToLower();
        if (ext == ".htm" || ext == ".html")
        {
            return true;
        }

        return false;
    }

    private static void WriteCliResponse(IAnsiConsole console, CliActionResponse response)
    {
        if (response.Plot != null)
        {
            var options = new Options
            {
                AxisColor = AnsiColor.Blue,
                LabelColor = AnsiColor.Blue,
                Height = 20
            };

            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine(AsciiChart.Sharp.AsciiChart.Plot(response.Plot.Series, options));
        }
        else if (response.Tables != null && response.Tables.Count != 0)
        {
            PrintTables(console, response);
        }
        else if (response.DumpObjects != null)
        {
            foreach (var item in response.DumpObjects)
            {
                DumpObject(console, item);
            }
        }
        else
        {
            WriteInfo(console, response);
        }
    }

    private static void DumpObject(IAnsiConsole console, CliDumpObject? obj)
    {
        if (obj == null) { return; }
        if (!string.IsNullOrWhiteSpace(obj.Title))
        {
            console.MarkupLine($"[black on white]{obj.Title}[/]");
        }

        CliObjectDumper.Dump(console, obj);
    }

    private static void PrintTables(IAnsiConsole console, CliActionResponse response)
    {
        if (response.Tables == null) { return; }
        foreach (var item in response.Tables)
        {
            PrintTableHeader(console, item);
            console.Write(item.Table);
            PrintTableFooter(console, item);

            console.WriteLine();
        }
    }

    private static void PrintTableHeader(IAnsiConsole console, CliTable item)
    {
        if (item.Title != null)
        {
            var rule = new Rule($"[aqua]{item.Title.EscapeMarkup()}[/]");
            rule.LeftJustified();
            console.Write(rule);
        }
    }

    private static void PrintTableFooter(IAnsiConsole console, CliTable item)
    {
        if (!item.ShowCount) { return; }
        var rows = item.Table.Rows.Count;
        if (rows <= 0) { return; }

        var entity = string.IsNullOrEmpty(item.EntityName) ? "row" : item.EntityName;
        var extra = rows > 1 ? "s" : string.Empty;
        var text = $"({rows} {entity}{extra})";
        if (item.Paging?.TotalPages > 1)
        {
            text += $" | page {item.Paging.PageNumber}/{item.Paging.TotalPages} | total {item.Paging.TotalRows:N0} {entity}s";
        }

        console.MarkupLine($" [black on gray] {text.EscapeMarkup()} [/]");
    }

    private static void HandleExceptionSafe(CliException ex, string? outputFilename)
    {
        try
        {
            if (ex.RestResponse != null)
            {
                var response = new CliActionResponse(ex.RestResponse);
                HandleCliResponse(response, outputFilename);
            }
            else
            {
                HandleException(ex);
            }
        }
        catch (Exception ex2)
        {
            WriteException(ex2);
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
            else if (finaleException is CliValidationException valEx && !string.IsNullOrWhiteSpace(valEx.Suggenstion))
            {
                MarkupCliLine(CliFormat.GetErrorMarkup(valEx.Message));
                AnsiConsole.WriteLine();
                var suggest = CliFormat.GetSuggestionMarkup(valEx.Suggenstion);
                MarkupCliLine(suggest);
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

        if (!string.IsNullOrEmpty(response.ErrorMessage))
        {
            MarkupCliLine(CliFormat.GetErrorMarkup(response.ErrorMessage));
        }
        else if (!string.IsNullOrEmpty(response.StatusDescription))
        {
            MarkupCliLine($"[red] ({response.StatusDescription})[/]");
        }
    }

    private static bool HandleHealthCheckResponse(RestResponse response)
    {
        if (response.StatusCode == HttpStatusCode.ServiceUnavailable &&
            response.Request != null &&
            response.Content != null &&
            response.Request.Resource.Contains("service/health-check", StringComparison.CurrentCultureIgnoreCase))
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
        if (HandleODataErrorResponse(response)) { return; }

        HandleGeneralError(response);
    }

    private static bool HandleODataErrorResponse(RestResponse response)
    {
        static string ClearMessage(string message)
        {
            var index = message.IndexOf("on type '");
            if (index < 0) { return message; }
            return message[0..index].ToLower();
        }

        if (response.StatusCode != HttpStatusCode.BadRequest) { return false; }
        if (string.IsNullOrWhiteSpace(response.Content)) { return false; }
        var token = JToken.Parse(response.Content);
        var message = token["error"]?["innererror"]?["message"]?.ToString();
        if (!string.IsNullOrWhiteSpace(message))
        {
            message = ClearMessage(message);
            MarkupCliLine(CliFormat.GetValidationErrorMarkup(message));
            return true;
        }

        message = token["error"]?["message"]?.ToString();
        if (!string.IsNullOrWhiteSpace(message))
        {
            message = ClearMessage(message);
            MarkupCliLine(CliFormat.GetValidationErrorMarkup(message));
            return true;
        }

        return false;
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

    private static void InteractiveMode(IEnumerable<CliActionMetadata> cliActions, bool showModules)
    {
        BaseCliAction.InteractiveMode = true;
        var command = string.Empty;

        if (showModules)
        {
            Console.Clear();
            CliHelpGenerator.ShowLogo();
            Console.WriteLine();
            CliHelpGenerator.ShowModules();
        }

        _timer = new Timer(OnTimerAction, null, _timerSpan, _timerSpan);

        while (true)
        {
            var color = ConnectUtil.Current.GetCliMarkupColor();
            var username = string.IsNullOrWhiteSpace(LoginProxy.Username) ? null : $"@{LoginProxy.Username}";
            AnsiConsole.Markup($"[{color}]{RestProxy.Host.EscapeMarkup()}:{RestProxy.Port}{username}[/]> ");
            command = Console.ReadLine();
            ResetTimer();

            var args = CommandSplitter.SplitCommandLine(command).ToArray();
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
            var args = noParameters ? [TokenManagerScope.Token] : new[] { param, TokenManagerScope.Token };
            if (action.Method.Invoke(console, args) is Task<CliActionResponse> task)
            {
                response = task.ConfigureAwait(false).GetAwaiter().GetResult();
            }
            else
            {
                Debugger.Break();
            }
        }
        catch (Exception ex)
        {
            throw new PlanarServiceException(ex);
        }

        return response;
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
            InteractiveMode(cliActions, showModules: true);
        }
        else
        {
            var cliUtil = HandleCliCommand(args, cliActions);
            if (cliUtil != null)
            {
                var command = $"{cliUtil.Module}.{cliUtil.Command}";
                if (string.Equals(command, "service.login", StringComparison.OrdinalIgnoreCase) && cliUtil.HasIterativeArgument)
                {
                    InteractiveMode(cliActions, showModules: false);
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

    private static void WriteInfo(IAnsiConsole console, CliActionResponse response)
    {
        var message = response.Message;
        if (!string.IsNullOrEmpty(message)) { message = message.Trim(); }
        if (message == "[]") { message = null; }
        if (string.IsNullOrEmpty(message)) { return; }

        if (response.FormattedMessage.GetValueOrDefault())
        {
            console.MarkupLine(message);
        }
        else
        {
            console.WriteLine(message);
        }
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