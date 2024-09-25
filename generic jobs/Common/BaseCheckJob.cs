namespace Common;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Job;
using Polly;
using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

public abstract class BaseCheckJob : BaseJob
{
    private static readonly object _locker = new();
    private readonly ConcurrentQueue<CheckException> _exceptions = new();
    private CheckFailCounter _failCounter = null!;
    private General _general = null!;
    private CheckSpanTracker _spanTracker = null!;

    protected static Dictionary<string, string> GetConnectionStrings(IConfiguration configuration)
    {
        var section = configuration.GetSection("connection strings");
        if (!section.Exists())
        {
            section = configuration.GetSection("ConnectionStrings");
        }

        if (!section.Exists())
        {
            section = configuration.GetSection("connectionStrings");
        }

        if (!section.Exists())
        {
            throw new InvalidDataException("coud not found any connection string in configuration");
        }

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
        ValidateGreaterThenOrEquals(@default.MaximumFailsInRow, 1, "maximum fails in row", section);
        ValidateLessThenOrEquals(@default.MaximumFailsInRow, 1000, "maximum fails in row", section);
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
            .Where(x => !string.IsNullOrEmpty(x.Name))
            .GroupBy(x => x.Name)
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
            throw new InvalidDataException($"'{fieldName}' field at '{section}' section is missing or empty");
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

    protected void Finilayze()
    {
        CheckAggragateException();
        HandleCheckExceptions();
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
        _failCounter = serviceProvider.GetRequiredService<CheckFailCounter>();

        var config = ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = ServiceProvider.GetRequiredService<ILogger<General>>();
        _general = new(config);
        ValidateGreaterThenOrEquals(_general.MaxDegreeOfParallelism, 2, "max degree of parallelism", "general");
        ValidateLessThenOrEquals(_general.MaxDegreeOfParallelism, 100, "max degree of parallelism", "general");
        logger.LogInformation(_general.ToString());
    }

    protected bool IsIntervalElapsed(ICheckElement element, TimeSpan? interval)
    {
        if (interval == null) { return true; }
        var tracker = ServiceProvider.GetRequiredService<CheckIntervalTracker>();
        var result = tracker.ShouldRun(element, interval.Value);
        return result;
    }

    protected async Task SafeInvokeCheck<T>(IEnumerable<T> entities, Func<T, Task> checkFunc)
                where T : BaseDefault, ICheckElement
    {
        if (_general.SequentialProcessing)
        {
            foreach (var entity in entities)
            {
                var status = await SafeInvokeCheck(entity, checkFunc);
                var notValidStatus = status != SafeHandleStatus.Success && status != SafeHandleStatus.CheckWarning;
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
            var tasks = entities.Select(x => new Func<Task>(() => SafeInvokeCheck(x, checkFunc)));
            await TaskQueue.RunAsync(tasks, _general.MaxDegreeOfParallelism);
        }
    }

    protected async Task<SafeHandleStatus> SafeInvokeCheck<T>(T entity, Func<T, Task> checkFunc)
        where T : BaseDefault, ICheckElement
    {
        try
        {
            if (entity.RetryCount == 0)
            {
                await checkFunc(entity);
                return SafeHandleStatus.Success;
            }

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

            _failCounter.ResetFailCount(entity);
            _spanTracker.ResetFailSpan(entity);

            return SafeHandleStatus.Success;
        }
        catch (Exception ex)
        {
            var status = SafeHandleCheckException(entity, ex);
            return status;
        }
    }

    protected IEnumerable<Task> SafeInvokeCheckInner<T>(IEnumerable<T> entities, Func<T, Task> checkFunc)
                where T : BaseDefault, ICheckElement
    {
        foreach (var item in entities)
        {
            yield return SafeInvokeCheck(item, checkFunc);
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

    protected IEnumerable<Task> SafeInvokeOperation<T>(IEnumerable<T> entities, Func<T, Task> operationFunc)
            where T : ICheckElement
    {
        foreach (var item in entities)
        {
            yield return SafeInvokeOperation(item, operationFunc);
        }
    }

    protected async Task SafeInvokeOperation<T>(T entity, Func<T, Task> operationFunc)
        where T : ICheckElement
    {
        try
        {
            await operationFunc(entity);
        }
        catch (Exception ex)
        {
            SafeHandleOperationException(entity, ex);
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

    private SafeHandleStatus SafeHandleCheckException<T>(T entity, Exception ex)
          where T : BaseDefault, ICheckElement
    {
        bool IsSpanValid()
        {
            return
                entity.Span != null &&
                entity.Span != TimeSpan.Zero &&
                entity.Span > _spanTracker.LastFailSpan(entity);
        }

        bool IsFailCounterInScope()
        {
            var failCount = _failCounter.IncrementFailCount(entity);
            return entity.MaximumFailsInRow.HasValue && failCount <= entity.MaximumFailsInRow;
        }

        try
        {
            if (!IsExceptionIsCheckException(ex, out var checkException))
            {
                Logger.LogError(ex, "check failed for '{Key}'. reason: {Message}",
                    entity.Key, ex.Message);
                AddAggregateException(ex);
                return SafeHandleStatus.Exception;
            }

            if (IsSpanValid())
            {
                Logger.LogWarning("check failed for '{Key}' but error span is valid. reason: {Message}",
                    entity.Key, ex.Message);
                return SafeHandleStatus.CheckWarning;
            }

            if (IsFailCounterInScope())
            {
                Logger.LogWarning("check failed for '{Key}' but maximum fails in row not reached yet. reason: {Message}",
                    entity.Key, ex.Message);
                return SafeHandleStatus.CheckWarning;
            }

            Logger.LogError("check failed for '{Key}'. reason: {Message}",
                entity.Key, ex.Message);

            AddCheckException(checkException);

            return SafeHandleStatus.CheckError;
        }
        catch (Exception innerEx)
        {
            AddAggregateException(innerEx);
            return SafeHandleStatus.Exception;
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
}