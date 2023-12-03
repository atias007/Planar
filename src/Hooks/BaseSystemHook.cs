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

        public void SetLogger(ILogger logger)
        {
            _logger = logger;
        }
    }
}