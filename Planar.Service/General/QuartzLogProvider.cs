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

                    var template = func();
                    if (logLevel == Microsoft.Extensions.Logging.LogLevel.Error)
                    {
#pragma warning disable CA2254 // Template should be a static expression

                        _logger.Log(logLevel, exception, template, parameters);
                    }
                    else
                    {
                        _logger.Log(logLevel, template, parameters);
#pragma warning restore CA2254 // Template should be a static expression
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