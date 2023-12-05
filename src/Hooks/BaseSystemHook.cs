using Microsoft.Extensions.Logging;
using Planar.Hook;

namespace Planar.Hooks;

public abstract class BaseSystemHook : BaseHook
{
    private ILogger? _logger;

    protected BaseSystemHook()
    {
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