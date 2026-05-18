using Microsoft.Extensions.Logging;
using System;

namespace Planar
{
    internal class ConsoleLogger<TContext> : ILogger<TContext>
    {
#if NETSTANDARD2_0

        public IDisposable BeginScope<TState>(TState state) => default;

#else
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default;
#endif

        public bool IsEnabled(LogLevel logLevel) => true;

#if NETSTANDARD2_0

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
#else
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
#endif
        {
            if (!IsEnabled(logLevel)) { return; }

            var message = $"<{typeof(TContext).Name}> {formatter(state, exception)}";
            Log(logLevel, message);

            if (exception != null)
            {
                message = $"<{typeof(TContext).Name}> {exception}";
                Log(logLevel, message);
            }
        }

        private static void Log(LogLevel logLevel, string message)
        {
            var log = new LogEntity { Level = logLevel, Message = message };
            if (logLevel == LogLevel.Error || logLevel == LogLevel.Critical)
            {
                Console.Error.WriteLine(log.ToString());
            }
            else
            {
                Console.Out.WriteLine(log.ToString());
            }
        }
    }
}