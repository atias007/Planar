using Microsoft.Extensions.Logging;
using Quartz.Logging;
using System;
using LogLevel = Quartz.Logging.LogLevel;

namespace Planar.Service.General
{
    public class QuartzLogProvider : ILogProvider
    {
        private readonly ILogger _logger;

        public QuartzLogProvider(ILogger logger)
        {
            _logger = logger;
        }

        public Logger GetLogger(string name)
        {
            return (level, func, exception, parameters) =>
            {
                if (level >= LogLevel.Info && func != null)
                {
                    var logLevel = level switch
                    {
                        LogLevel.Debug or LogLevel.Trace => Microsoft.Extensions.Logging.LogLevel.Debug,
                        LogLevel.Info => Microsoft.Extensions.Logging.LogLevel.Information,
                        LogLevel.Warn => Microsoft.Extensions.Logging.LogLevel.Warning,
                        LogLevel.Error => Microsoft.Extensions.Logging.LogLevel.Error,
                        LogLevel.Fatal => Microsoft.Extensions.Logging.LogLevel.Critical,
                        _ => Microsoft.Extensions.Logging.LogLevel.Trace,
                    };

                    if (logLevel == Microsoft.Extensions.Logging.LogLevel.Error)
                    {
                        _logger.Log(logLevel, exception, string.Format(func(), parameters));
                    }
                    else
                    {
                        _logger.Log(logLevel, string.Format(func(), parameters));
                    }
                }
                return true;
            };
        }

        public IDisposable OpenNestedContext(string message)
        {
            return null;
        }

        public IDisposable OpenMappedContext(string key, object value, bool destructure = false)
        {
            return null;
        }
    }
}