using Microsoft.Extensions.Logging;
using Planar.Monitor.Hook;

namespace Planar.Hooks
{
    public abstract class BaseSystemHook : BaseHook
    {
        private ILogger? _logger;

        protected BaseSystemHook()
        {
        }

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
}