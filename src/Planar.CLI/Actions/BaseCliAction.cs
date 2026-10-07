using Microsoft.AspNetCore.DataProtection;
using Planar.API.Common.Entities;
using Planar.CLI.Attributes;
using Planar.CLI.CliGeneral;
using Planar.CLI.Entities;
using Planar.CLI.General;
using Planar.CLI.Proxy;
using Planar.Common;
using RestSharp;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.CLI.Actions;

public class CollectCliValueParameters
{
    public required string Field { get; init; }
    public bool Required { get; init; }
    public int MinLength { get; init; }
    public int? MaxLength { get; init; }
    public string? Regex { get; init; }
    public string? RegexErrorMessage { get; init; }
    public string? DefaultValue { get; init; }
    public bool Secret { get; init; }
    public Func<string, ValidationResult>? Validation { get; init; }
}

public abstract class BaseCliAction
{
    protected const string JobFileName = "JobFile.yml";

    public static bool InteractiveMode { get; set; }

    protected static async Task<CliActionResponse> Apply(string kind, CliApplyRequest request, CancellationToken cancellationToken = default)
    {
        request ??= new CliApplyRequest();

        if (string.IsNullOrWhiteSpace(request.Filename))
        {
            request.Filename = CollectCliValue(new CollectCliValueParameters
            {
                Field = "file/path",
                Required = true,
                MinLength = 2,
                MaxLength = 500
            }) ?? string.Empty;
        }

        var pathInfo = PathAnalyzer.AnalyzePath(request.Filename);
        IEnumerable<string> files;
        if (pathInfo.IsFolder)
        {
            files = Directory.EnumerateFiles(pathInfo.Path, pathInfo.Pattern, SearchOption.TopDirectoryOnly);
        }
        else
        {
            files = [pathInfo.Path];
        }

        var sb = new List<string>();
        var counter = 0;
        foreach (var file in files)
        {
            var fi = new FileInfo(file);
            if (
                !string.Equals(fi.Extension, ".yml", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(fi.Extension, ".yaml", StringComparison.OrdinalIgnoreCase)) { continue; }

            var (content, success) = await SafeReadFile(file, cancellationToken);
            if (success)
            {
                counter++;
                if (counter > 100) { throw new CliWarningException("apply command can handle no more then 100 files"); }

                AnsiConsole.MarkupLine($"[gray] > read file {file.EscapeMarkup()} ({fi.Length:N0} bytes)[/]");
                content = AddSourceFilenameToYmlContent(content, fi.Name);
                sb.Add(content);
            }
            else
            {
                AnsiConsole.MarkupLine($"[gray] > read file {file.EscapeMarkup()} ({fi.Length:N0} bytes)[/] [red]error read file. skip apply. message: {content.EscapeMarkup()}[/]");
            }
        }

        if (counter == 0)
        {
            AnsiConsole.MarkupLine($"[gray] > no yml files found. skip apply[/]");
            return CliActionResponse.Empty;
        }

        var body = string.Join("\r\n---\r\n", sb).Trim();
        if (counter > 1)
        {
            AnsiConsole.MarkupLine($"[gray] --- total {counter} file(s) ---[/]");
        }

        AnsiConsole.MarkupLine("[gray] > send apply request...[/]");
        var restRequestAdd = new RestRequest($"{kind}/apply", Method.Post)
            .AddStringBody(body, CliConsts.YamlContentType);

        var resultApply = await RestProxy.Invoke<CliApplyResponse>(restRequestAdd, cancellationToken);
        var tables = CliTableExtensions.GetTable(resultApply.Data);
        return new CliActionResponse(resultApply, tables);
    }

    protected static void ValidateFileExists(string filename)
    {
        var fi = new FileInfo(filename);

        if (!fi.Exists)
        {
            throw new CliException($"file '{fi.FullName}' does not exists");
        }
    }

    protected static void ValidateFileSize(string filename, int maxSizeBytes = 1024 * 1000)
    {
        var fi = new FileInfo(filename);

        if (fi.Length > maxSizeBytes)
        {
            throw new CliException($"file '{fi.FullName}' size ({fi.Length:N0}) is over the allowed max file size ({maxSizeBytes:N0})");
        }
    }

    protected static void ValidateTextFile(string filename, int maxSizeBytes = 1024 * 1000)
    {
        var valid = IsProbablyText(filename, maxSizeBytes);

        if (!valid)
        {
            throw new CliException($"file '{filename}' is not a valid simple text format (file is binary)");
        }
    }

    protected static bool IsProbablyText(string filename, int sampleSize = 8192)
    {
        using FileStream fs = File.OpenRead(filename);
        byte[] buffer = new byte[sampleSize];
        int read = fs.Read(buffer, 0, buffer.Length);
        if (read == 0) return true;                  // empty → treat as text

        ReadOnlySpan<byte> data = buffer.AsSpan(0, read);

        // 1. BOM → definitely text
        if (HasBom(data)) return true;

        // 2. NUL byte → almost certainly binary (git's rule)
        if (data.IndexOf((byte)0) >= 0) return false;

        // 3. Must decode as valid UTF-8
        try
        {
            new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(data);
        }
        catch (DecoderFallbackException)
        {
            return false;
        }

        // 4. Too many control chars → binary
        int control = 0;
        foreach (byte b in data)
            if (b < 0x20 && b != 0x09 && b != 0x0A && b != 0x0D) control++;

        return control * 100 / read < 5;
    }

    private static bool HasBom(ReadOnlySpan<byte> d) =>
       (d.Length >= 3 && d[0] == 0xEF && d[1] == 0xBB && d[2] == 0xBF) ||  // UTF-8
       (d.Length >= 2 && d[0] == 0xFF && d[1] == 0xFE) ||                  // UTF-16 LE
       (d.Length >= 2 && d[0] == 0xFE && d[1] == 0xFF);                    // UTF-16 BE

    protected static async Task<CliActionResponse> Execute(RestRequest request, CancellationToken cancellationToken = default)
    {
        var result = await RestProxy.Invoke(request, cancellationToken);
        return new CliActionResponse(result);
    }

    protected static void FillMissingDataProperties(ICliDataRequest request)
    {
        if (request.Action == null)
        {
            var items = Enum.GetNames<DataActions>()
                .Select(n => n.ToLower())
                .OrderBy(n => n);

            var action = CliPromptUtil.PromptSelection(items, "action") ?? string.Empty;
            request.Action = Enum.Parse<DataActions>(action, true);
        }

        if (request.Action == DataActions.Clear) { return; }

        if (string.IsNullOrWhiteSpace(request.DataKey))
        {
            request.DataKey = CollectCliValue(new CollectCliValueParameters
            {
                Field = "key",
                Required = true,
                MinLength = 1,
                MaxLength = 100
            }) ?? string.Empty;
        }

        if (request.Action == DataActions.Remove) { return; }

        if (string.IsNullOrWhiteSpace(request.DataValue) && request.Action == DataActions.Put)
        {
            request.DataValue = CollectCliValue(new CollectCliValueParameters
            {
                Field = "value",
                Required = false,
                MinLength = 0,
                MaxLength = 1000
            });
        }
    }

    protected static void FillBool<T>(T entity, string propertyName, bool defaultValue = false)
        where T : class
    {
        var info = ReflectionHelper.GetPropertyInfo<T>(propertyName);
        var attribute = ReflectionHelper.GetActionPropertyAttribute<T>(propertyName);
        var displayName = attribute.InputDisplayName ?? attribute.DisplayName ?? string.Empty;
        if (string.IsNullOrWhiteSpace(displayName)) { displayName = propertyName; }
        var result = CollectBoolCliValue(displayName, defaultValue);
        info.SetValue(entity, result);
    }

    protected static void FillOptionalString<T>(T entity, string propertyName, int maxValue, string? defaultValue = null, bool secret = false)
        where T : class
    {
        var tuple = CollectText(entity, propertyName, false, -1, maxValue, defaultValue, secret);
        if (tuple.Item1)
        {
            if (string.IsNullOrWhiteSpace(tuple.Item2)) { tuple.Item2 = null; }
            tuple.Item3.SetValue(entity, tuple.Item2);
        }
    }

    protected static void FillRequiredString<T>(T entity, string propertyName, int minLength, int maxLength, string? defaultValue = null, bool secret = false)
        where T : class
    {
        var tuple = CollectText(entity, propertyName, true, minLength, maxLength, defaultValue, secret);
        if (tuple.Item1)
        {
            tuple.Item3.SetValue(entity, tuple.Item2);
        }
    }

    protected static void FillRequiredFilename<T>(T entity, string propertyName, bool allowFolder, params string[] allowedExtensions)
        where T : class
    {
        var tuple = CollectText(entity, propertyName,
            required: true,
            minLength: 1,
            maxLength: 500,
            defaultValue: null,
            secret: false,
            validation: (value) => ValidateFile(value, allowFolder, allowedExtensions));

        if (tuple.Item1)
        {
            tuple.Item3.SetValue(entity, tuple.Item2);
        }

        static ValidationResult ValidateFile(string value, bool allowFolder, string[] allowedExtensions)
        {
            string path;
            try
            {
                path = NormalizePath(value);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                return GetValidationResultError("invalid path format");
            }

            if (!allowFolder && Directory.Exists(path))
            {
                return GetValidationResultError("that's a directory, not a file");
            }

            if (!File.Exists(path))
            {
                return GetValidationResultError($"file not found: {Markup.Escape(path)}");
            }

            if (allowedExtensions.Length > 0 &&
                !allowedExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            {
                return GetValidationResultError($"expected: {string.Join(", ", allowedExtensions)}");
            }

            return ValidationResult.Success();
        }

        static string NormalizePath(string input)
        {
            var p = input.Trim().Trim('"', '\'');
            p = Environment.ExpandEnvironmentVariables(p); // %USERPROFILE%\...
            if (p.StartsWith('~'))
            {
                p = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), p[1..].TrimStart('/', '\\'));
            }

            return Path.GetFullPath(p); // resolves relative paths
        }
    }

    protected static void FillRequiredInt<T>(T entity, string propertyName, string? defaultValue = null, bool secret = false)
        where T : class
    {
        var tuple = CollectText(entity, propertyName, true, 1, 10, defaultValue, secret,
            v =>
            {
                if (!int.TryParse(v, out _))
                {
                    return GetValidationResultError("invalid number");
                }

                return ValidationResult.Success();
            }
        );

        if (tuple.Item1 && int.TryParse(tuple.Item2, out var intValue))
        {
            tuple.Item3.SetValue(entity, intValue);
        }
    }

    protected static void FillRequiredLong<T>(T entity, string propertyName, string? defaultValue = null, bool secret = false)
        where T : class
    {
        var tuple = CollectText(entity, propertyName, true, 1, 25, defaultValue, secret,
            v =>
            {
                if (!long.TryParse(v, out _))
                {
                    return GetValidationResultError("invalid number");
                }

                return ValidationResult.Success();
            }
        );

        if (tuple.Item1 && long.TryParse(tuple.Item2, out var intValue))
        {
            tuple.Item3.SetValue(entity, intValue);
        }
    }

    protected static void FillRequiredTimeSpan<T>(T entity, string propertyName, string? defaultValue = null, bool secret = false)
        where T : class
    {
        var tuple = CollectText(entity, propertyName, true, 1, 20, defaultValue, secret,
            v =>
            {
                if (!TimeSpan.TryParse(v, CultureInfo.CurrentCulture, out _))
                {
                    return GetValidationResultError("invalid time span value");
                }

                return ValidationResult.Success();
            }
        );

        if (tuple.Item1 && TimeSpan.TryParse(tuple.Item2, CultureInfo.CurrentCulture, out var tsValue))
        {
            tuple.Item3.SetValue(entity, tsValue);
        }
    }

    private static (bool, string?, PropertyInfo) CollectText<T>(T entity, string propertyName, bool required, int minLength, int maxLength, string? defaultValue, bool secret, Func<string, ValidationResult>? validation = null)
    where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        var info = ReflectionHelper.GetPropertyInfo<T>(propertyName);
        var value = info.GetValue(entity)?.ToString();
        var attribute = ReflectionHelper.GetActionPropertyAttribute<T>(propertyName);
        var displayName = attribute.InputDisplayName ?? attribute.DisplayName ?? string.Empty;
        if (string.IsNullOrWhiteSpace(displayName)) { displayName = propertyName; }

        if (IsEmpty(value, info.PropertyType)) { value = string.Empty; }

        if (string.IsNullOrWhiteSpace(value))
        {
            value = CollectCliValue(new CollectCliValueParameters
            {
                Field = displayName.ToLower(),
                Required = required,
                MinLength = minLength,
                MaxLength = maxLength,
                DefaultValue = defaultValue,
                Secret = secret,
                Validation = validation
            }) ?? string.Empty;

            return (true, value, info);
        }

        return (false, null, info);
    }

    private static bool IsEmpty(string? value, Type type)
    {
        const string zero = "0";

        var isNumeric = type.IsPrimitive &&
             (type == typeof(int) || type == typeof(double) ||
              type == typeof(float) || type == typeof(byte) ||
              type == typeof(sbyte) || type == typeof(short) ||
              type == typeof(ushort) || type == typeof(long) ||
              type == typeof(ulong));

        if (isNumeric)
        {
            return value == zero;
        }

        if (type == typeof(TimeSpan) && TimeSpan.TryParse(value, CultureInfo.CurrentCulture, out var ts))
        {
            return ts == TimeSpan.Zero;
        }

        return string.IsNullOrWhiteSpace(value);
    }

    protected static int? CollectNumericCliValue(string field, bool required, int minValue, int maxValue, int? defaultValue = null)
    {
        var prompt = new TextPrompt<string>($"[turquoise2]  > {field.EscapeMarkup()?.Trim()}:[/]")
            .Validate(value =>
            {
                if (required && string.IsNullOrWhiteSpace(value)) { return GetValidationResultError($"{field} is required field"); }
                value = value.Trim();

                if (string.IsNullOrEmpty(value) && !required)
                {
                    return ValidationResult.Success();
                }

                if (!int.TryParse(value, out var num))
                {
                    return GetValidationResultError($"{field} must be a valid integer number");
                }

                if (num > maxValue) { return GetValidationResultError($"{field} limited to maximum value of {maxValue}"); }
                if (num < minValue) { return GetValidationResultError($"{field} limited to minimum value of {minValue}"); }

                return ValidationResult.Success();
            });

        if (!required) { prompt.AllowEmpty(); }
        if (defaultValue.HasValue)
        {
            prompt.DefaultValue(defaultValue.GetValueOrDefault().ToString(CultureInfo.CurrentCulture));
        }

        var console = new EscCancelConsole(AnsiConsole.Console);
        var result = console.Prompt(prompt);
        return int.Parse(result);
    }

    protected static bool ConfirmCliValue(CollectCliValueParameters parameters)
    {
        var prompt = new ConfirmationPrompt($"[turquoise2]  > {parameters.Field.EscapeMarkup()?.Trim()}:[/]");
        var console = new EscCancelConsole(AnsiConsole.Console);
        var result = console.Prompt(prompt);
        return result;
    }

    protected static bool CollectBoolCliValue(string field, bool defaultValue = false)
    {
        var prompt = new ConfirmationPrompt($"[turquoise2]  > {field.EscapeMarkup()?.Trim()}:[/]")
        {
            DefaultValue = defaultValue
        };
        var console = new EscCancelConsole(AnsiConsole.Console);
        var result = console.Prompt(prompt);
        return result;
    }

    protected static bool AskForUpdateField(string field, string currentValue, bool defaultValue = false)
    {
        var prompt = new ConfirmationPrompt($"[turquoise2] do you want to update: {field.EscapeMarkup()?.Trim()}?[/] [gray]({currentValue.EscapeMarkup()?.Trim()})[/]")
        {
            DefaultValue = defaultValue
        };
        var console = new EscCancelConsole(AnsiConsole.Console);
        var result = console.Prompt(prompt);
        return result;
    }

#pragma warning disable S3776 // Cognitive Complexity of methods should not be too high

    protected static string? CollectCliValue(CollectCliValueParameters parameters)
#pragma warning restore S3776 // Cognitive Complexity of methods should not be too high
    {
        var prompt = new TextPrompt<string>($"[turquoise2]  > {parameters.Field.EscapeMarkup()?.Trim()}:[/]")
            .Validate(value =>
            {
                if (parameters.Required && string.IsNullOrWhiteSpace(value)) { return GetValidationResultError($"{parameters.Field} is required field"); }
                value = value.Trim();

                if (string.IsNullOrEmpty(value) && !parameters.Required)
                {
                    return ValidationResult.Success();
                }

                if (parameters.MaxLength.HasValue && value.Length > parameters.MaxLength) { return GetValidationResultError($"{parameters.Field} limited to {parameters.MaxLength} chars maximum"); }
                if (value.Length < parameters.MinLength) { return GetValidationResultError($"{parameters.Field} must have at least {parameters.MinLength} chars"); }
                if (!string.IsNullOrEmpty(parameters.Regex))
                {
                    var rx = new Regex(parameters.Regex, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
                    if (!rx.IsMatch(value))
                    {
                        return GetValidationResultError($"{parameters.Field} {parameters.RegexErrorMessage}");
                    }
                }

                if (parameters.Validation != null)
                {
                    var customValidation = parameters.Validation.Invoke(value);
                    if (!customValidation.Successful)
                    {
                        return customValidation;
                    }
                }

                return ValidationResult.Success();
            });

        if (!parameters.Required) { prompt.AllowEmpty(); }
        if (parameters.Secret) { prompt.Secret(); }
        if (!string.IsNullOrEmpty(parameters.DefaultValue))
        {
            prompt.DefaultValue(parameters.DefaultValue);
        }

        var console = new EscCancelConsole(AnsiConsole.Console);
        var result = console.Prompt(prompt);

        return string.IsNullOrEmpty(result) ? null : result;
    }

    protected static async Task<CliActionResponse> ExecuteEntity<T>(RestRequest request, CancellationToken cancellationToken)
    {
        var result = await RestProxy.Invoke<T>(request, cancellationToken);
        if (result.IsSuccessful)
        {
            return new CliActionResponse(result, dumpObject: result.Data);
        }

        return new CliActionResponse(result);
    }

    protected static async Task<CliActionResponse> ExecuteTable<T>(RestRequest request, Func<T, CliTable> tableFunc, CancellationToken cancellationToken)
        where T : class
    {
        var result = await RestProxy.Invoke<T>(request, cancellationToken);
        if (result.IsSuccessful && result.Data != null)
        {
            var table = tableFunc.Invoke(result.Data);
            return new CliActionResponse(result, table);
        }

        return new CliActionResponse(result);
    }

    private static ValidationResult GetValidationResultError(string message)
    {
        var markup = CliFormat.GetErrorMarkup(message);
        return ValidationResult.Error(markup);
    }

    public static IEnumerable<CliActionMetadata> GetAllActions()
    {
        var modules = GetModules();
        var result = new List<CliActionMetadata>();
        result.AddRange(InnerCliActions.GetActions());

        foreach (var item in modules)
        {
            result.AddRange(item.Actions);
        }

        return result;
    }

    private static IEnumerable<CliModule>? _modules;
    private static CliModule? _innerModule;

    public static CliModule GetInnerModule()
    {
        if (_innerModule != null)
        {
            return _innerModule;
        }

        _innerModule = GetModule<InnerCliActions>();
        return _innerModule;
    }

    public static IEnumerable<CliModule> GetModules()
    {
        if (_modules != null)
        {
            return _modules;
        }

        _modules = new List<CliModule>
        {
            GetModule<JobCliActions>(),
            GetModule<ServiceCliActions>(),
            GetModule<TriggerCliActions>(),
            GetModule<TraceCliActions>(),
            GetModule<ConfigCliActions>(),
            GetModule<HistoryCliActions>(),
            GetModule<UserCliActions>(),
            GetModule<GroupCliActions>(),
            GetModule<ClusterCliActions>(),
            GetModule<MonitorCliActions>(),
            GetModule<MetricsCliActions>(),
            GetModule<ReportCliActions>(),
            GetModule<ResourceCliActions>()
        }
        .OrderBy(m => m.Name);

        return _modules;
    }

    private static CliModule GetModule<T>()
        where T : BaseCliAction<T>
    {
        var result = new CliModule();
        var type = typeof(T);

        // actions
        var method = type.GetMethod("GetActions",
                BindingFlags.Static |
                BindingFlags.FlattenHierarchy |
                BindingFlags.InvokeMethod |
                BindingFlags.Public);

        if (method?.Invoke(null, null) is IEnumerable<CliActionMetadata> actions)
        {
            result.Actions = actions;
        }

        // name and description
        var attribute = type.GetCustomAttribute<ModuleAttribute>();
        if (attribute != null)
        {
            result.Name = attribute.Name;
            result.Description = attribute.Description;
        }

        return result;
    }

    protected static void AssertCreated(RestResponse<PlanarIdResponse> response)
    {
        if (!response.IsSuccessful) { return; }
        Util.SetLastJobOrTriggerId(response);
        if (string.IsNullOrWhiteSpace(response.Data?.Id))
        {
            AnsiConsole.MarkupLine("no change");
        }
        else
        {
            Console.WriteLine(response.Data?.Id);
        }
    }

    protected static void AssertCreated(RestResponse<PlanarIdResponseWrapper> response)
    {
        if (!response.IsSuccessful) { return; }
        Util.SetLastJobOrTriggerId(response);
        if (response.Data?.Unchanged == true || string.IsNullOrWhiteSpace(response.Data?.PlanarId?.Id))
        {
            AnsiConsole.MarkupLine("no change");
        }
        else
        {
            Console.WriteLine(response.Data?.PlanarId?.Id);
        }
    }

    protected static void AssertJobUpdated(RestResponse<PlanarIdResponse> response)
    {
        if (!response.IsSuccessful) { return; }
        Util.SetLastJobOrTriggerId(response);
    }

    protected static void AssertTriggerUpdated(RestResponse response, string id)
    {
        if (!response.IsSuccessful) { return; }
        AssertUpdated(id, "trigger");
    }

    protected static void AssertUpdated(string? id, string entity)
    {
        if (string.IsNullOrEmpty(id)) { return; }
        string message = entity switch
        {
            "job" => CliFormat.GetWarningMarkup("job is in 'pause' state and none of its triggers will fire"),
            "trigger" => CliFormat.GetWarningMarkup("trigger is in 'pause' state and it will not fire"),
            _ => string.Empty,
        };

        if (!string.IsNullOrEmpty(message))
        {
            AnsiConsole.MarkupLine(message);
        }
    }

    protected static string? PromptSelection(IEnumerable<string>? items, string title, bool writeSelection = true)
    {
        return CliPromptUtil.PromptSelection(items, title, writeSelection);
    }

    protected static CliSelectItem<T>? PromptSelection<T>(IEnumerable<CliSelectItem<T>>? items, string title)
    {
        return CliPromptUtil.PromptSelection(items, title);
    }

    protected static TEnum PromptSelection<TEnum>(string title)
        where TEnum : struct, Enum
    {
        var items = Enum.GetNames<TEnum>().Select(e => e.ToLower());
        var result = CliPromptUtil.PromptSelection(items, title);
        return Enum.Parse<TEnum>(result!, true);
    }

    protected static bool ConfirmAction(string title)
    {
        if (!InteractiveMode) { return true; }
        return AnsiConsole.Confirm($"are you sure that you want to {title}?", false);
    }

    protected static int GetCounterHours()
    {
        var items = new[] { "1 hour", "2 hours", "8 hours", "1 day", "2 days", "3 days", "7 days" };
        var select = PromptSelection(items, "select time period");
        if (string.IsNullOrEmpty(select)) { return 0; }
        var parts = select.Split(' ');
        if (parts.Length != 2) { return 0; }
        if (!int.TryParse(parts[0], out var num)) { return 0; }
        if (parts[1].Length < 1) { return 0; }
        if (parts[1][0] == 'h') { return num; }
        if (parts[1][0] == 'd') { return num * 24; }
        return 0;
    }

    protected static void FillDatesScope(ICliDateScope request)
    {
        if (request.FromDate == default && request.ToDate == default)
        {
            var dates = GetDateScope();
            request.FromDate = dates.Item1 ?? default;
            request.ToDate = dates.Item2 ?? default;
        }
    }

    private static (DateTime?, DateTime?) GetDateScope()
    {
        var items = new[] { "today", "yesterday", "this week", "last week", "this month", "last month", "this year", "last year", "since forever", "custom..." };
        var select = PromptSelection(items, "select date period");

        if (string.Equals(select, "custom...", StringComparison.OrdinalIgnoreCase))
        {
            return GetCustomDateScope();
        }

        (DateTime?, DateTime?) result = select switch
        {
            "today" => (DateTime.Today, null),
            "yesterday" => (DateTime.Today.AddDays(-1), DateTime.Today),
            "this week" => (DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek), DateTime.Today.AddDays(7 - (int)DateTime.Today.DayOfWeek)),
            "last week" => (DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek - 7), DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek)),
            "this month" => (DateTime.Today.AddDays(-DateTime.Today.Day + 1), DateTime.Today.AddDays(-DateTime.Today.Day + 1).AddMonths(1)),
            "last month" => (DateTime.Today.AddDays(-DateTime.Today.Day + 1).AddMonths(-1), DateTime.Today.AddDays(-DateTime.Today.Day + 1)),
            "this year" => (DateTime.Today.AddDays(-DateTime.Today.DayOfYear + 1), DateTime.Today.AddDays(-DateTime.Today.DayOfYear + 1).AddYears(1)),
            "last year" => (DateTime.Today.AddDays(-DateTime.Today.DayOfYear + 1).AddYears(-1), DateTime.Today.AddDays(-DateTime.Today.DayOfYear + 1)),
            "since forever" => (null, null),
            _ => (null, null)
        };

        PrintSummaryDate("from date:", result.Item1);
        PrintSummaryDate("to date:  ", result.Item2);

        return result;
    }

    private static void PrintSummaryDate(string title, DateTime? date)
    {
        if (date == null)
        {
            AnsiConsole.MarkupLine($"[turquoise2]  > {title.EscapeMarkup()} [/] [[empty]]");
        }
        else
        {
            var format = CliActionMetadata.GetCurrentDateTimeFormat();
            var value = date.Value.ToString(format);
            AnsiConsole.MarkupLine($"[turquoise2]  > {title.EscapeMarkup()} [/] {value}");
        }
    }

    private static (DateTime?, DateTime?) GetCustomDateScope()
    {
        var from = CliPromptUtil.PromptForDate("from date");
        var to = CliPromptUtil.PromptForDate("to date  ");
        return (from, to);
    }

    private static async Task<(string Content, bool Success)> SafeReadFile(string filename, CancellationToken cancellationToken)
    {
        try
        {
            var content = await File.ReadAllTextAsync(filename, cancellationToken);
            return string.IsNullOrWhiteSpace(content) ? ("file is empty", false) : (content, true);
        }
        catch (Exception ex)
        {
            return (ex.Message, false);
        }
    }

    private static string AddSourceFilenameToYmlContent(string ymlContent, string sourceFilename)
    {
        const string separator = "---";
        const string source = "source: ";

        var items = YmlUtil.SplitByKind(ymlContent);
        var final = new StringBuilder();
        foreach (var item in items)
        {
            var content = item.Value.Trim();
            if (string.IsNullOrWhiteSpace(content)) { continue; }
            if (content == separator) { continue; }
            if (content.StartsWith(separator)) { content = content[separator.Length..]; }
            if (content.EndsWith(separator)) { content = content[0..(content.Length - separator.Length)]; }
            final.AppendLine($"{source}{sourceFilename}");
            final.AppendLine(content.Trim());
            final.AppendLine(separator);
        }

        var result = final.ToString().Trim();
        if (result.EndsWith(separator))
        {
            result = result[0..(result.Length - separator.Length)];
        }

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

        foreach (var act in allActions)
        {
            var actionAttributes = act.GetCustomAttributes<ActionAttribute>();
            var nullRequestAttribute = act.GetCustomAttribute<NullRequestAttribute>();
            var ignoreHelpAttribute = act.GetCustomAttribute<IgnoreHelpAttribute>();

            if (actionAttributes == null || !actionAttributes.Any()) { continue; }

            var requestType = GetRequestType(act);
            var commands = actionAttributes.Select(a => a.Name).Distinct().ToList();
            var item = new CliActionMetadata
            {
                Module = moduleAttribute?.Name?.ToLower() ?? string.Empty,
                Method = act,
                Commands = commands,
                AllowNullRequest = nullRequestAttribute != null,
                RequestType = requestType,
                Arguments = GetArguments(requestType),
                CommandDisplayName = string.Join('|', commands.OrderBy(c => c.Length)),
                IgnoreHelp = ignoreHelpAttribute != null,
            };

            if (!string.IsNullOrEmpty(moduleAttribute?.Synonyms))
            {
                item.ModuleSynonyms = [.. moduleAttribute.Synonyms.Split(',')];
            }

            if (!string.IsNullOrEmpty(item.Module))
            {
                item.ModuleSynonyms.Add(item.Module);
            }

            item.SetArgumentsDisplayName();

            result.Add(item);
        }

        return result;
    }

    private static Type? GetRequestType(MethodInfo? method)
    {
        if (method == null) { return null; }

        var parameters = method.GetParameters();
        if (parameters.Length == 0)
        {
            throw new CliException($"cli error: action '{method.Name}' has no parameters");
        }

        if (parameters.Length > 2)
        {
            throw new CliException($"cli error: action '{method.Name}' has more then 2 parameter");
        }

        var last = parameters[^1];
        if (last.ParameterType != typeof(CancellationToken))
        {
            throw new CliException($"cli error: action '{method.Name}' has no CancellationToken parameter");
        }

        var requestType =
            parameters.Length == 1 ?
            null :
            parameters[0].ParameterType;

        return requestType;
    }

    private static List<CliArgumentMetadata> GetArguments(Type? requestType)
    {
        var result = new List<CliArgumentMetadata>();
        if (requestType == null)
        {
            return result;
        }

        var props = requestType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        var isJobKey =
            requestType.IsAssignableFrom(typeof(CliJobKey)) ||
            requestType.IsSubclassOf(typeof(CliJobKey));

        var isTriggerKey =
            requestType.IsAssignableFrom(typeof(CliTriggerKey)) ||
            requestType.IsSubclassOf(typeof(CliTriggerKey));

        foreach (var item in props)
        {
            var att = item.GetCustomAttribute<ActionPropertyAttribute>();
            var req = item.GetCustomAttribute<RequiredAttribute>();
            var info = new CliArgumentMetadata
            {
                PropertyInfo = item,
                LongName = att?.LongName?.ToLower(),
                ShortName = att?.ShortName?.ToLower(),
                DisplayName = att?.DisplayName,
                Default = (att?.Default).GetValueOrDefault(),
                Required = req != null,
                RequiredMissingMessage = req?.Message,
                DefaultOrder = (att?.DefaultOrder).GetValueOrDefault(),
                JobKey = isJobKey && item.Name == nameof(CliJobKey.Id),
                TriggerKey = isTriggerKey && item.Name == nameof(CliTriggerKey.Id),
            };
            result.Add(info);
        }

        return result;
    }
}