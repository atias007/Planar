namespace Common;

using Planar.Job;
using System.Collections.Concurrent;

public abstract class BaseCheckJob : BaseJob
{
    private readonly ConcurrentQueue<CheckException> _exceptions = new();

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

    protected static void SetDefault<TSource, TTarget, TProperty>(TSource source, TTarget target, Func<TProperty?> func)
    {
        var propertyName = func.Method.Name[4..]; // Remove "get_" prefix
        var sourceValue = func();
        if (Equals(sourceValue, default(TProperty?)))
        {
            var targetPropertyInfo = typeof(TTarget).GetProperty(propertyName);
            targetPropertyInfo?.SetValue(target, sourceValue);
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

    protected static void FillBase(BaseDefault baseDefaultTarget, BaseDefault baseDefaultSorce)
    {
        baseDefaultTarget.RetryInterval ??= baseDefaultSorce.RetryInterval;
        baseDefaultTarget.RetryCount ??= baseDefaultSorce.RetryCount;
        baseDefaultTarget.MaximumFailsInRow ??= baseDefaultSorce.MaximumFailsInRow;
    }

    protected virtual void HandleCheckExceptions(string entity)
    {
        if (!_exceptions.IsEmpty)
        {
            var message = $"{entity} check failed for {entity}(s): {string.Join(", ", _exceptions.Select(x => x.Key).Distinct())}";
            throw new AggregateException(message, _exceptions);
        }
    }

    protected virtual void HandleCheckExceptions(string check, string elements)
    {
        if (!_exceptions.IsEmpty)
        {
            var message = $"{check} check failed for {elements}(s): {string.Join(", ", _exceptions.Select(x => x.Key).Distinct())}";
            throw new AggregateException(message, _exceptions);
        }
    }

    protected void AddCheckException(CheckException exception)
    {
        _exceptions.Enqueue(exception);
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

    protected static void ValidateMaxLength(string? value, double limit, string fieldName, string section)
    {
        if (string.IsNullOrWhiteSpace(value)) { return; }

        if (value.Length > limit)
        {
            throw new InvalidDataException($"'{fieldName}' field on '{section}' section must be less then or equals {limit:N0}");
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
}