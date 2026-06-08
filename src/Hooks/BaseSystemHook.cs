using Microsoft.Extensions.Logging;
using Planar.Hook;

namespace Planar.Hooks;

public interface ISystemHook
{
    string Name { get; }
    string Description { get; }
    ILogger Logger { get; }
}

public abstract class BaseSystemHook(ILogger logger) : BaseHook, ISystemHook
{
    public ILogger Logger => logger;

    protected static string? GetParameter(string name, IMonitorGroup group)
    {
        if (group == null) { return null; }

        var fields = new[]
        {
            group.AdditionalField1,
            group.AdditionalField2,
            group.AdditionalField3,
            group.AdditionalField4,
            group.AdditionalField5,
        };

        foreach (var item in fields)
        {
            var value = GetParameter(name, item);
            if (!string.IsNullOrWhiteSpace(value)) { return value; }
        }

        return null;
    }

    protected static string? GetParameter(string name, string? addtionalField)
    {
        if (string.IsNullOrWhiteSpace(addtionalField)) { return null; }
        if (!addtionalField.ToLower().StartsWith(name.ToLower() + ":")) { return null; }
        var value = addtionalField[(name.Length + 1)..];
        if (string.IsNullOrWhiteSpace(value)) { return null; }

        return value;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "INF")]
    protected override void LogError(string message)
    {
        if (logger == null)
        {
            base.LogError(message);
        }
        else
        {
            logger.LogError(message);
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "INF")]
    protected override void LogCritical(string message)
    {
        if (logger == null)
        {
            base.LogCritical(message);
        }
        else
        {
            logger.LogCritical(message);
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "INF")]
    protected override void LogDebug(string message)
    {
        if (logger == null)
        {
            base.LogDebug(message);
        }
        else
        {
            logger.LogDebug(message);
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "INF")]
    protected override void LogInformation(string message)
    {
        if (logger == null)
        {
            base.LogInformation(message);
        }
        else
        {
            logger.LogInformation(message);
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "INF")]
    protected override void LogTrace(string message)
    {
        if (logger == null)
        {
            base.LogTrace(message);
        }
        else
        {
            logger.LogTrace(message);
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "INF")]
    protected override void LogWarning(string message)
    {
        if (logger == null)
        {
            base.LogWarning(message);
        }
        else
        {
            logger.LogWarning(message);
        }
    }
}