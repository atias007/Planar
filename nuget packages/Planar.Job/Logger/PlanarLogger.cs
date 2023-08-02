using Microsoft.Extensions.Logging;
using System;

namespace Planar.Job.Logger
{
    internal class BaseLogger
    {
#pragma warning disable IDE0060 // Remove unused parameter

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default;

#pragma warning restore IDE0060 // Remove unused parameter

#pragma warning disable IDE0060 // Remove unused parameter

        public bool IsEnabled(LogLevel logLevel) => true;

#pragma warning restore IDE0060 // Remove unused parameter

        protected void LogToConsole(string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Out.WriteLineAsync(message);
            Console.ForegroundColor = ConsoleColor.White;
        }

        protected string GetLogLevelDisplayTest(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Trace => "TRC",
                LogLevel.Debug => "DBG",
                LogLevel.Information => "INF",
                LogLevel.Warning => "WRN",
                LogLevel.Error => "ERR",
                LogLevel.Critical => "CRT",
                _ => "NON", // case LogLevel.None
            };
        }
    }

    internal class PlanarLogger<TContext> : BaseLogger, ILogger<TContext>
    {
        public PlanarLogger() : base()
        {
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) { return; }

            var message = $"[{DateTime.Now:HH:mm:ss} {GetLogLevelDisplayTest(logLevel)}] <{typeof(TContext).Name}> {formatter(state, exception)}";
            var entity = new LogEntity { Message = message, Level = logLevel };
            MqttClient.Publish(MessageBrokerChannels.AppendLog, entity).ConfigureAwait(false).GetAwaiter().GetResult();
            LogToConsole(message);
        }
    }

    internal class PlanarLogger : BaseLogger, ILogger
    {
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) { return; }

            var message = $"[{DateTime.Now:HH:mm:ss} {GetLogLevelDisplayTest(logLevel)}] {formatter(state, exception)}";
            var entity = new LogEntity { Message = message, Level = logLevel };
            MqttClient.Publish(MessageBrokerChannels.AppendLog, entity).ConfigureAwait(false).GetAwaiter().GetResult();
            LogToConsole(message);
        }
    }
}