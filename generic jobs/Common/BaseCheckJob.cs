using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Job;
using Polly;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace Common;

public abstract class BaseCheckJob : BaseJob
{
    private static readonly object _locker = new();
    private readonly ConcurrentQueue<CheckException> _exceptions = new();
    private General _general = null!;
    private CheckSpanTracker _spanTracker = null!;

    protected static Dictionary<string, string> GetConnectionStrings(IConfiguration configuration, List<string> names)
    {
        var sections = new string[] { "connection strings", "ConnectionStrings" };
        var dic = new Dictionary<string, string>();
        foreach (var item in sections)
        {
            var section = configuration.GetSection(item);
            if (section.Exists())
            {
                var connStrings = ReadConnectionStringFromSection(section);
                foreach (var s in connStrings)
                {
                    dic.TryAdd(s.Key.ToLower(), s.Value);
                }
            }
        }

        var result = new Dictionary<string, string>();
        foreach (var name in names)
        {
            var lowerName = name.ToLower();
            var value = dic.GetValueOrDefault(lowerName) ?? string.Empty;
            result.TryAdd(name, value);
        }

        return result;
    }

    protected static IConfigurationSection? GetDefaultSection(IConfiguration configuration, ILogger logger)
    {
        var defaults = configuration.GetSection("defaults");
        if (defaults == null)
        {
            logger.LogWarning("no defaults section found on settings file. set job factory defaults");
            return null;
        }

        return defaults;
    }

    protected static void ValidateAtLeastOneRequired<T>(IEnumerable<T> values, IEnumerable<string> fieldNames, string section)
    {
        foreach (var value in values)
        {
            var stringValue = Convert.ToString(value, CultureInfo.CurrentCulture);
            if (!string.IsNullOrWhiteSpace(stringValue)) { return; }
        }

        var fields = string.Join(", ", fieldNames);
        throw new InvalidDataException($"on of fields: {fields} at '{section}' section is required");
    }

    protected static void ValidateBase(BaseDefault @default, string section)
    {
        ValidateRequired(@default.RetryInterval, "retry interval", section);
        ValidateGreaterThenOrEquals(@default.RetryInterval?.TotalSeconds, 1, "retry interval", section);
        ValidateLessThen(@default.RetryInterval?.TotalMinutes, 1, "retry interval", section);
        ValidateGreaterThenOrEquals(@default.RetryCount, 0, "retry count", section);
        ValidateLessThenOrEquals(@default.RetryCount, 10, "retry count", section);
        ValidateGreaterThen(@default.AllowedFailSpan, TimeSpan.FromSeconds(1), "allowed fail span", section);
    }

    protected static void ValidateDuplicateKeys<T>(IEnumerable<T> items, string sectionName)
                            where T : ICheckElement
    {
        var duplicates1 = items
            .Where(x => !string.IsNullOrEmpty(x.Key))
            .GroupBy(x => x.Key)
            .Where(g => g.Count() > 1)
            .Select(y => y.Key)
            .ToList();

        if (duplicates1.Count != 0)
        {
            throw new InvalidDataException($"duplicated found at '{sectionName}' section. duplicate keys found: {string.Join(", ", duplicates1)}");
        }
    }

    protected static void ValidateDuplicateNames<T>(IEnumerable<T> items, string sectionName)
        where T : INamedCheckElement
    {
        var duplicates1 = items
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .GroupBy(x => x.Name)
            .Where(g => g.Count() > 1)
            .Select(y => y.Key)
            .ToList();

        if (duplicates1.Count != 0)
        {
            throw new InvalidDataException($"duplicated found at '{sectionName}' section. duplicate names found: {string.Join(", ", duplicates1)}");
        }
    }

    protected static void ValidateDuplicates(IEnumerable<string> items, string sectionName)
    {
        var duplicates1 = items
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .GroupBy(x => x)
            .Where(g => g.Count() > 1)
            .Select(y => y.Key)
            .ToList();

        if (duplicates1.Count != 0)
        {
            throw new InvalidDataException($"duplicated found at '{sectionName}' section. duplicate names found: {string.Join(", ", duplicates1)}");
        }
    }

    protected static void ValidateGreaterThen(double? value, double limit, string fieldName, string section)
    {
        if (value == null) { return; }

        if (value <= limit)
        {
            throw new InvalidDataException($"'{fieldName}' field with value {value} at '{section}' section must be greater then {limit:N0}");
        }
    }

    protected static void ValidateGreaterThen(TimeSpan? value, TimeSpan limit, string fieldName, string section)
    {
        if (value == null) { return; }

        if (value <= limit)
        {
            throw new InvalidDataException($"'{fieldName}' field with value {FormatTimeSpan(value.Value)} at '{section}' section must be greater then {FormatTimeSpan(limit)}");
        }
    }

    protected static void ValidateGreaterThenOrEquals(double? value, double limit, string fieldName, string section)
    {
        if (value == null) { return; }

        if (value < limit)
        {
            throw new InvalidDataException($"'{fieldName}' field with value {value} at '{section}' section must be greater then {limit:N0}");
        }
    }

    protected static void ValidateLessThen(double? value, double limit, string fieldName, string section)
    {
        if (value == null) { return; }

        if (value > limit)
        {
            throw new InvalidDataException($"'{fieldName}' field with value {value} at '{section}' section must be less then {limit:N0}");
        }
    }

    protected static void ValidateLessThen(TimeSpan? value, TimeSpan limit, string fieldName, string section)
    {
        if (value == null) { return; }

        if (value >= limit)
        {
            throw new InvalidDataException($"'{fieldName}' field with value {FormatTimeSpan(value.Value)} at '{section}' section must be less then {FormatTimeSpan(limit)}");
        }
    }

    protected static void ValidateLessThenOrEquals(double? value, double limit, string fieldName, string section)
    {
        if (value == null) { return; }

        if (value >= limit)
        {
            throw new InvalidDataException($"'{fieldName}' field with value {value} at '{section}' section must be less then {limit:N0}");
        }
    }

    protected static void ValidateMaxLength(string? value, double limit, string fieldName, string section)
    {
        if (string.IsNullOrWhiteSpace(value)) { return; }

        if (value.Length > limit)
        {
            throw new InvalidDataException($"'{fieldName}' field on '{section}' section must be less then or equals {limit:N0}");
        }
    }

    protected static void ValidateNullOrWhiteSpace(IEnumerable<string>? items, string sectionName)
    {
        var has = items?.Any(x => string.IsNullOrWhiteSpace(x)) ?? false;

        if (has)
        {
            throw new InvalidDataException($"null or empty items found at '{sectionName}' section");
        }
    }

    protected static void ValidatePathExists(string path)
    {
        try
        {
            var directory = new DirectoryInfo(path);
            if (!directory.Exists)
            {
                throw new CheckException($"directory '{path}' not found");
            }
        }
        catch (Exception ex)
        {
            throw new CheckException($"directory '{path}' is invalid ({ex.Message})");
        }
    }

    protected static void ValidateRequired(object? value, string fieldName, string section)
    {
        var stringValue = Convert.ToString(value, CultureInfo.CurrentCulture);
        if (string.IsNullOrWhiteSpace(stringValue))
        {
            throw new InvalidDataException($"'{fieldName}' field at '{section}' section is missing");
        }
    }

    protected static void ValidateRequired<T>(IEnumerable<T>? value, string fieldName, string section = "root")
    {
        if (value == null || !value.Any())
        {
            throw new InvalidDataException($"'{fieldName}' section is missing or empty");
        }
    }

    protected static void ValidateUri(string value, string fieldName, string section)
    {
        ValidateMaxLength(value, 1000, fieldName, section);

        if (!Uri.TryCreate(value, UriKind.Absolute, out _))
        {
            throw new InvalidDataException($"'{fieldName}' field with value '{value}' at '{section}' section is not valid uri");
        }
    }

    protected void AddCheckException(CheckException exception)
    {
        _exceptions.Enqueue(exception);
    }

    protected bool CheckRequired<T>(IEnumerable<T>? value, string name)
    {
        if (value == null || !value.Any())
        {
            Logger.LogWarning("no {Name} to run", name);
            return false;
        }

        return true;
    }

    protected bool CheckVeto(IVetoEntity entity, string entityName)
    {
        if (entity.Veto && string.IsNullOrWhiteSpace(entity.VetoReason))
        {
            Logger.LogInformation("{Name} '{Key}' has veto", entityName, entity.Key);
        }

        if (entity.Veto)
        {
            Logger.LogInformation("{Name} '{Key}' has veto. reason: {Reason}", entityName, entity.Key, entity.VetoReason);
        }

        return entity.Veto;
    }

    protected void Finalayze()
    {
        CheckAggragateException();
        HandleCheckExceptions();
    }

    protected FinalayzeDetails<T> GetFinalayzeDetails<T>(T data)
    {
        var success = ExceptionCount == 0 && _exceptions.IsEmpty;
        var details = new FinalayzeDetails<T>(data, this, success);
        return details;
    }

    protected static IEnumerable<string>? GetKeys(IJobExecutionContext context)
    {
        if (!context.MergedJobDataMap.TryGet("keys", out var keys)) { return null; }
        if (string.IsNullOrWhiteSpace(keys)) { return null; }

        var result = keys.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .ToList();

        return result;
    }

    protected IReadOnlyDictionary<string, HostsConfig> GetHosts(IConfiguration configuration, Action<Host> veto)
    {
        var dic = new Dictionary<string, HostsConfig>();
        var hosts = configuration.GetSection("hosts");
        if (hosts == null) { return dic; }
        foreach (var host in hosts.GetChildren())
        {
            var result = new HostsConfig(host);

            if (result.Hosts == null || !result.Hosts.Any())
            {
                throw new InvalidDataException($"fail to read 'hosts' of group name '{result.GroupName}' under 'hosts' main section. list is null or empty");
            }

            if (!dic.TryAdd(result.GroupName, result))
            {
                throw new InvalidDataException($"fail to read 'group name' under 'hosts' section with duplicate value '{result.GroupName}'");
            }

            var vetoHosts = result.VetoHosts(veto);
            LogVetoHost(vetoHosts);
        }

        return dic;
    }

    protected void IncreaseEffectedRows()
    {
        lock (_locker)
        {
            EffectedRows = EffectedRows.GetValueOrDefault() + 1;
        }
    }

    [SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "Infrastructure")]
    protected void Initialize(IServiceProvider serviceProvider)
    {
        _spanTracker = serviceProvider.GetRequiredService<CheckSpanTracker>();

        var config = ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = ServiceProvider.GetRequiredService<ILogger<General>>();
        _general = new(config);
        ValidateGreaterThenOrEquals(_general.MaxDegreeOfParallelism, 2, "max degree of parallelism", "general");
        ValidateLessThenOrEquals(_general.MaxDegreeOfParallelism, 100, "max degree of parallelism", "general");
        logger.LogInformation(_general.ToString());
    }

    protected Task SafeInvokeCheck<T>(IEnumerable<T> entities, Action<T> checkAction, ITriggerDetail trigger)
        where T : BaseDefault, ICheckElement
    {
        var asyncFunc = async (T entity) => await Task.Run(() => checkAction(entity));
        return SafeInvokeCheck(entities, asyncFunc, trigger);
    }

    protected async Task SafeInvokeCheck<T>(IEnumerable<T> entities, Func<T, Task> checkFunc, ITriggerDetail trigger)
                where T : BaseDefault, ICheckElement
    {
        if (_general.SequentialProcessing)
        {
            foreach (var entity in entities)
            {
                await SafeInvokeCheck(entity, checkFunc, trigger);
                var notValidStatus = entity.RunStatus.IsInvalidStatus();
                if (_general.StopRunningOnFail && notValidStatus)
                {
                    var ex = new InvalidOperationException("stop running on fail is enabled. job will stop running");
                    await AddAggregateExceptionAsync(ex);
                    break;
                }
            }
        }
        else
        {
            var tasks = entities.Select(x => new Func<Task>(() => SafeInvokeCheck(x, checkFunc, trigger)));
            await TaskQueue.RunAsync(tasks, _general.MaxDegreeOfParallelism);
        }
    }

    protected async Task SafeInvokeCheck<T>(T entity, Func<T, Task> checkFunc, ITriggerDetail trigger)
        where T : BaseDefault, ICheckElement
    {
        try
        {
            if (IsInactiveCheck(entity)) { return; }
            if (IsUnbindCheckTriggers(entity, trigger)) { return; }

            if (entity.RetryCount == 0)
            {
                await checkFunc(entity);
            }
            else
            {
                await Policy.Handle<Exception>()
                        .WaitAndRetryAsync(
                            retryCount: entity.RetryCount.GetValueOrDefault(),
                            sleepDurationProvider: _ => entity.RetryInterval.GetValueOrDefault(),
                             onRetry: (ex, _) =>
                             {
                                 Logger.LogWarning("retry for '{Key}'. Reason: {Message}", entity.Key, ex.Message);
                             })
                        .ExecuteAsync(async () =>
                        {
                            await checkFunc(entity);
                        });
            }

            _spanTracker.ResetFailSpan(entity);
            entity.RunStatus = CheckStatus.Success;
        }
        catch (Exception ex)
        {
            entity.RunStatus = SafeHandleCheckException(entity, ex);
        }
    }

    protected async Task<T?> SafeInvokeFunction<T>(Func<Task<T>> func, BaseDefault baseDefault)
    {
        try
        {
            if (baseDefault.RetryCount == 0)
            {
                return await func();
            }

            return await Policy.Handle<Exception>()
                    .WaitAndRetryAsync(
                        retryCount: baseDefault.RetryCount.GetValueOrDefault(),
                        sleepDurationProvider: _ => baseDefault.RetryInterval.GetValueOrDefault(),
                         onRetry: (ex, _) =>
                         {
                             var exception = ex is CheckException ? null : ex;
                             Logger.LogWarning(exception, "retry invoked. Reason: {Message}", ex.Message);
                         })
                    .ExecuteAsync(async () =>
                    {
                        return await func();
                    });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "unhandled exception. reason: {Message}", ex.Message);
            await AddAggregateExceptionAsync(ex);
        }

        return default;
    }

    protected Task SafeInvokeOperation<T>(IEnumerable<T> entities, Action<T> operationAction, ITriggerDetail trigger)
        where T : BaseOperation, ICheckElement
    {
        var asyncFunc = async (T entity) => await Task.Run(() => operationAction(entity));
        return SafeInvokeOperation(entities, asyncFunc, trigger);
    }

    protected async Task SafeInvokeOperation<T>(IEnumerable<T> entities, Func<T, Task> operationFunc, ITriggerDetail trigger)
            where T : BaseOperation, ICheckElement
    {
        if (_general.SequentialProcessing)
        {
            var total = entities.Count();
            var current = 0;
            foreach (var entity in entities)
            {
                current++;
                await SafeInvokeOperation(entity, operationFunc, trigger);
                await UpdateProgressAsync(current, total);
                var notValidStatus = entity.RunStatus.IsValidStatus();
                if (_general.StopRunningOnFail && notValidStatus)
                {
                    var ex = new InvalidOperationException("stop running on fail is enabled. job will stop running");
                    await AddAggregateExceptionAsync(ex);
                    break;
                }
            }
        }
        else
        {
            var tasks = entities.Select(x => new Func<Task>(() => SafeInvokeOperation(x, operationFunc, trigger)));
            await TaskQueue.RunAsync(tasks, _general.MaxDegreeOfParallelism);
        }
    }

    private static string FormatTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan.TotalDays >= 1)
        {
            return $"{timeSpan:\\(d\\)\\ hh\\:mm\\:ss}";
        }

        return $"{timeSpan:hh\\:mm\\:ss}";
    }

    private static bool IsExceptionIsCheckException(Exception ex, [NotNullWhen(true)] out CheckException? checkException)
    {
        if (ex is CheckException checkException1)
        {
            checkException = checkException1;
            return true;
        }

        if (ex.InnerException is CheckException checkException2)
        {
            checkException = checkException2;
            return true;
        }

        if (
            ex is AggregateException aggregateException &&
            aggregateException.InnerExceptions.Count == 1 &&
            aggregateException.InnerExceptions[0] is CheckException checkException3)
        {
            checkException = checkException3;
            return true;
        }

        checkException = null;
        return false;
    }

    private static Dictionary<string, string> ReadConnectionStringFromSection(IConfigurationSection section)
    {
        var result = new Dictionary<string, string>();
        foreach (var item in section.GetChildren())
        {
            if (string.IsNullOrWhiteSpace(item.Key))
            {
                throw new InvalidDataException("connection string has invalid null or empty key");
            }

            if (string.IsNullOrWhiteSpace(item.Value))
            {
                throw new InvalidDataException($"connection string with key '{item.Key}' has no value");
            }

            result.TryAdd(item.Key, item.Value);
        }

        return result;
    }

    private void HandleCheckExceptions()
    {
        if (!_exceptions.IsEmpty)
        {
            if (_exceptions.Count == 1 && _exceptions.TryDequeue(out var exception))
            {
                throw exception;
            }

            var sb = new StringBuilder(_exceptions.Count + 1);
            sb.AppendLine(CultureInfo.CurrentCulture, $"there is {_exceptions.Count} check fails. see details below:");
            foreach (var ex in _exceptions)
            {
                sb.AppendLine(CultureInfo.CurrentCulture, $" - {ex.Message}");
            }

            throw new CheckException(sb.ToString());
        }
    }

    private bool IsInactiveCheck<T>(T entity)
            where T : BaseDefault, ICheckElement
    {
        var inactive = !entity.Active;
        if (inactive)
        {
            Logger.LogInformation("skipping inactive check: '{Key}'", entity.Key);
            entity.RunStatus = CheckStatus.Inactive;
        }

        return inactive;
    }

    private bool IsInactiveOperation<T>(T entity)
       where T : BaseOperation, ICheckElement
    {
        var inactive = !entity.Active;
        if (inactive)
        {
            Logger.LogInformation("skipping inactive operation: '{Name}'", entity.Key);
            entity.RunStatus = OperationStatus.Inactive;
        }

        return inactive;
    }

    private bool IsUnbindCheckTriggers<T>(T entity, ITriggerDetail trigger)

      where T : BaseDefault, ICheckElement
    {
        if (trigger.Key.Name.StartsWith("MT_")) { return false; }
        if (entity.BindToTriggers == null) { return false; }
        if (!entity.BindToTriggers.Any()) { return false; }

        var bindNotIncludeCurrentTrigger = !entity.BindToTriggers.Any(t => string.Equals(t, trigger.Key.Name, StringComparison.OrdinalIgnoreCase));
        if (bindNotIncludeCurrentTrigger)
        {
            Logger.LogInformation("skipping check '{Key}' due to the 'bind to triggers' list is not include '{Trigger}'", entity.Key, trigger.Key.Name);
            entity.RunStatus = CheckStatus.Ignore;
            return true;
        }

        return false;
    }

    private bool IsUnbindOperationTriggers<T>(T entity, ITriggerDetail trigger)
        where T : BaseOperation, ICheckElement
    {
        var hasBindToTriggers = entity.BindToTriggers != null && entity.BindToTriggers.Any();
        var bindNotIncludeCurrentTrigger = hasBindToTriggers && !entity.BindToTriggers!.Any(t => string.Equals(t, trigger.Key.Name, StringComparison.OrdinalIgnoreCase));

        if (bindNotIncludeCurrentTrigger)
        {
            Logger.LogInformation("skipping operation '{Key}' due to the 'bind to triggers' list is not include '{Trigger}'", entity.Key, trigger.Key.Name);
            entity.RunStatus = OperationStatus.Ignore;
            return true;
        }

        return false;
    }

    private void LogVetoHost(IEnumerable<Host> hosts)
    {
        foreach (var item in hosts)
        {
            if (string.IsNullOrEmpty(item.VetoReason))
            {
                Logger.LogInformation("host {Host} veto", item.Name);
            }
            else
            {
                Logger.LogInformation("host {Host} veto. reason: {Reason}", item.Name, item.VetoReason);
            }
        }
    }

    private CheckStatus SafeHandleCheckException<T>(T entity, Exception ex)
          where T : BaseDefault, ICheckElement
    {
        try
        {
            if (!IsExceptionIsCheckException(ex, out var checkException))
            {
                Logger.LogError(ex, "check failed for '{Key}'. reason: {Message}",
                    entity.Key, ex.Message);
                AddAggregateException(ex);
                return CheckStatus.Exception;
            }

            if (_spanTracker.IsSpanValid(entity))
            {
                Logger.LogWarning("check failed for '{Key}' but error span is valid. reason: {Message}",
                    entity.Key, ex.Message);

                return CheckStatus.CheckWarning;
            }

            Logger.LogError("check failed for '{Key}'. reason: {Message}",
                entity.Key, ex.Message);

            AddCheckException(checkException);

            return CheckStatus.CheckError;
        }
        catch (Exception innerEx)
        {
            AddAggregateException(innerEx);
            return CheckStatus.Exception;
        }
    }

    private void SafeHandleOperationException<T>(T entity, Exception ex)
                                    where T : ICheckElement
    {
        try
        {
            if (ex is not CheckException checkException)
            {
                Logger.LogError(ex, "operation failed for '{Key}'. reason: {Message}", entity.Key, ex.Message);
                AddAggregateException(ex);
            }
            else
            {
                Logger.LogError("check failed for '{Key}'. reason: {Message}", entity.Key, ex.Message);
                AddCheckException(checkException);
            }
        }
        catch (Exception innerEx)
        {
            AddAggregateException(innerEx);
        }
    }

    private async Task SafeInvokeOperation<T>(T entity, Func<T, Task> operationFunc, ITriggerDetail trigger)
                        where T : BaseOperation, ICheckElement
    {
        try
        {
            if (IsInactiveOperation(entity)) { return; }
            if (IsUnbindOperationTriggers(entity, trigger)) { return; }

            await operationFunc(entity);
            entity.RunStatus = OperationStatus.Success;
        }
        catch (Exception ex)
        {
            SafeHandleOperationException(entity, ex);
            entity.RunStatus = OperationStatus.Exception;
        }
    }
}