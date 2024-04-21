namespace Common;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Job;
using Polly;
using System.Collections.Concurrent;
using System.Text;

public abstract class BaseCheckJob : BaseJob
{
    private readonly ConcurrentQueue<CheckException> _exceptions = new();
    private CheckFailCounter _counter = null!;
    private CheckSpanTracker _spanner = null!;

    protected static void FillBase(BaseDefault baseDefaultTarget, BaseDefault baseDefaultSorce)
    {
        baseDefaultTarget.RetryInterval ??= baseDefaultSorce.RetryInterval;
        baseDefaultTarget.RetryCount ??= baseDefaultSorce.RetryCount;
        baseDefaultTarget.MaximumFailsInRow ??= baseDefaultSorce.MaximumFailsInRow;
        baseDefaultTarget.Span ??= baseDefaultSorce.Span;
    }

    protected static void FillBase(IEnumerable<BaseDefault> baseDefaultTargets, BaseDefault baseDefaultSorce)
    {
        foreach (var item in baseDefaultTargets)
        {
            FillBase(item, baseDefaultSorce);
        }
    }

    protected static void SetDefault<TTarget, TProperty>(TTarget target, Func<TProperty?> func)
    {
        var propertyName = func.Method.Name[4..]; // Remove "get_" prefix
        var sourceValue = func();
        if (Equals(sourceValue, default(TProperty?)))
        {
            var targetPropertyInfo = typeof(TTarget).GetProperty(propertyName);
            targetPropertyInfo?.SetValue(target, sourceValue);
        }
    }

    protected static void SetDefaultName<T>(T entity, Func<string> func)
    {
        const string noname = "[no name]";
        var value = func();
        var stringValue = value?.Trim();
        var propertyName = func.Method.Name[4..]; // Remove "get_" prefix
        var propertyInfo = typeof(T).GetProperty(propertyName);
        if (string.IsNullOrWhiteSpace(stringValue))
        {
            propertyInfo?.SetValue(entity, noname);
        }
        else
        {
            propertyInfo?.SetValue(entity, stringValue);
        }
    }

    protected static void ValidateBase(BaseDefault @default, string section)
    {
        ValidateRequired(@default.RetryInterval, "retry interval", section);
        ValidateGreaterThen(@default.RetryInterval?.TotalSeconds, 1, "retry interval", section);
        ValidateLessThen(@default.RetryInterval?.TotalMinutes, 1, "retry interval", section);
        ValidateGreaterThenOrEquals(@default.RetryCount, 0, "retry count", section);
        ValidateLessThenOrEquals(@default.RetryCount, 10, "retry count", section);
        ValidateGreaterThenOrEquals(@default.MaximumFailsInRow, 1, "maximum fails in row", section);
        ValidateLessThenOrEquals(@default.MaximumFailsInRow, 1000, "maximum fails in row", section);
    }

    protected static void ValidateGreaterThen(double? value, double limit, string fieldName, string section)
    {
        if (value == null) { return; }

        if (value <= limit)
        {
            throw new InvalidDataException($"'{fieldName}' field with value {value} at '{section}' section must be greater then {limit:N0}");
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

    protected static void ValidateRequired(object? value, string fieldName, string section)
    {
        var stringValue = Convert.ToString(value);
        if (string.IsNullOrWhiteSpace(stringValue))
        {
            throw new InvalidDataException($"'{fieldName}' field at '{section}' section is missing");
        }
    }

    protected static void ValidateRequired<T>(IEnumerable<T>? value, string fieldName, string section)
    {
        if (value == null || !value.Any())
        {
            throw new InvalidDataException($"'{fieldName}' field at '{section}' section is missing or empty");
        }
    }

    protected static void ValidateUri(string value, string fieldName, string section)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out _))
        {
            throw new InvalidDataException($"'{fieldName}' field with value '{value}' at '{section}' section is not valid uri");
        }
    }

    protected void AddCheckException(CheckException exception)
    {
        _exceptions.Enqueue(exception);
    }

    protected virtual void HandleCheckExceptions()
    {
        if (!_exceptions.IsEmpty)
        {
            if (_exceptions.Count == 1 && _exceptions.TryDequeue(out var exception))
            {
                throw exception;
            }

            var sb = new StringBuilder(_exceptions.Count + 1);
            sb.AppendLine($"there is {_exceptions.Count} check fails. see details below:");
            foreach (var ex in _exceptions)
            {
                sb.AppendLine($" - {ex.Message}");
            }

            throw new CheckException(sb.ToString());
        }
    }

    protected void Initialize(IServiceProvider serviceProvider)
    {
        _spanner = serviceProvider.GetRequiredService<CheckSpanTracker>();
        _counter = serviceProvider.GetRequiredService<CheckFailCounter>();
    }

    protected void SafeHandleException<T>(T entity, Exception ex)
      where T : BaseDefault, ICheckElemnt
    {
        bool IsSpanValid()
        {
            return
                entity.Span != null &&
                entity.Span != TimeSpan.Zero &&
                entity.Span > _spanner.LastFailSpan(entity);
        }

        bool IsCounterValid()
        {
            var failCount = _counter.IncrementFailCount(entity);
            return
                entity.MaximumFailsInRow.HasValue &&
                failCount <= entity.MaximumFailsInRow;
        }

        try
        {
            if (ex is not CheckException checkException)
            {
                Logger.LogError(ex, "check failed for key '{Key}'. reason: {Message}", entity.Key, ex.Message);
                AddAggregateException(ex);
                return;
            }

            if (IsSpanValid())
            {
                Logger.LogWarning("check failed but error span is valid for key '{Key}'. reason: {Message}", entity.Key, ex.Message);
                return;
            }

            if (IsCounterValid())
            {
                Logger.LogWarning("check failed but maximum fails in row reached for key '{Key}'. reason: {Message}",
                    entity.Key, ex.Message);
                return;
            }

            AddCheckException(checkException);
        }
        catch (Exception innerEx)
        {
            AddAggregateException(innerEx);
        }
    }

    protected IEnumerable<Task> SafeInvokeCheck<T>(IEnumerable<T> entities, Func<T, Task> checkFunc)
            where T : BaseDefault, ICheckElemnt
    {
        foreach (var item in entities)
        {
            yield return SafeInvokeCheck(item, checkFunc);
        }
    }

    protected async Task SafeInvokeCheck<T>(T entity, Func<T, Task> checkFunc)
        where T : BaseDefault, ICheckElemnt
    {
        try
        {
            if (entity.RetryCount == 0)
            {
                await checkFunc(entity);
                return;
            }

            await Policy.Handle<Exception>()
                    .WaitAndRetryAsync(
                        retryCount: entity.RetryCount.GetValueOrDefault(),
                        sleepDurationProvider: _ => entity.RetryInterval.GetValueOrDefault(),
                         onRetry: (ex, _) =>
                         {
                             var exception = ex is CheckException ? null : ex;
                             Logger.LogWarning(exception, "retry for key '{Key}'. Reason: {Message}", entity.Key, ex.Message);
                         })
                    .ExecuteAsync(async () =>
                    {
                        await checkFunc(entity);
                    });

            _counter.ResetFailCount(entity);
            _spanner.ResetFailSpan(entity);
        }
        catch (Exception ex)
        {
            SafeHandleException(entity, ex);
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
            AddAggregateException(ex);
        }

        return default;
    }
}