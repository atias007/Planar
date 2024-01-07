using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Hook;
using System;

namespace Planar.Hooks;

public abstract class BaseSystemHook : BaseHook
{
    private ILogger? _logger;

    protected BaseSystemHook()
    {
    }

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
        if (_logger == null)
        {
            base.LogError(message);
        }
        else
        {
            _logger.LogError(message);
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "INF")]
    protected override void LogCritical(string message)
    {
        if (_logger == null)
        {
            base.LogCritical(message);
        }
        else
        {
            _logger.LogCritical(message);
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "INF")]
    protected override void LogDebug(string message)
    {
        if (_logger == null)
        {
            base.LogDebug(message);
        }
        else
        {
            _logger.LogDebug(message);
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "INF")]
    protected override void LogInformation(string message)
    {
        if (_logger == null)
        {
            base.LogInformation(message);
        }
        else
        {
            _logger.LogInformation(message);
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "INF")]
    protected override void LogTrace(string message)
    {
        if (_logger == null)
        {
            base.LogTrace(message);
        }
        else
        {
            _logger.LogTrace(message);
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "INF")]
    protected override void LogWarning(string message)
    {
        if (_logger == null)
        {
            base.LogWarning(message);
        }
        else
        {
            _logger.LogWarning(message);
        }
    }

    public void SetLogger(ILogger logger)
    {
        _logger = logger;
    }
}