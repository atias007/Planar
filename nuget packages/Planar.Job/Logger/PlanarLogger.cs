using Microsoft.Extensions.Logging;
using System;

namespace Planar.Job.Logger
{
    internal class BaseLogger
    {
        private readonly MessageBroker _messageBroker;

        public BaseLogger(MessageBroker messageBroker)
        {
            _messageBroker = messageBroker;
        }

        protected MessageBroker MessageBroker => _messageBroker;

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
    }

    internal class PlanarLogger<TContext> : BaseLogger, ILogger<TContext>
    {
        public PlanarLogger(MessageBroker messageBroker) : base(messageBroker)
        {
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) { return; }

            var message = $"[{DateTime.Now:HH:mm:ss} {logLevel}] | {typeof(TContext).Name} | {formatter(state, exception)}";
            var entity = new LogEntity { Message = message, Level = logLevel };
            MessageBroker.Publish(MessageBrokerChannels.AppendLog, entity);
            LogToConsole(message);
        }
    }

    internal class PlanarLogger : BaseLogger, ILogger
    {
        public PlanarLogger(MessageBroker messageBroker) : base(messageBroker)
        {
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) { return; }

            var message = $"[{DateTime.Now:HH:mm:ss} {logLevel}] {formatter(state, exception)}";
            var entity = new LogEntity { Message = message, Level = logLevel };
            MessageBroker.Publish(MessageBrokerChannels.AppendLog, entity);
            LogToConsole(message);
        }
    }
}