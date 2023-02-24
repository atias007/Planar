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

        public IDisposable BeginScope<TState>(TState state) => default;

        public bool IsEnabled(LogLevel logLevel) => true;

        protected void LogToConsole(string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Out.WriteLineAsync(message);
            Console.ForegroundColor = ConsoleColor.White;
        }
    }

    internal class PlannerLogger<TContext> : BaseLogger, ILogger<TContext>
    {
        public PlannerLogger(MessageBroker messageBroker) : base(messageBroker)
        {
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel)) { return; }

            var message = $"[{DateTime.Now:HH:mm:ss} {logLevel}] | {typeof(TContext).Name} | {formatter(state, exception)}";
            var entity = new LogEntity { Message = message, Level = logLevel };
            MessageBroker.Publish(MessageBrokerChannels.AppendLog, entity);
            LogToConsole(message);
        }
    }

    internal class PlannerLogger : BaseLogger, ILogger
    {
        public PlannerLogger(MessageBroker messageBroker) : base(messageBroker)
        {
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel)) { return; }

            var message = $"[{DateTime.Now:HH:mm:ss} {logLevel}] {formatter(state, exception)}";
            var entity = new LogEntity { Message = message, Level = logLevel };
            MessageBroker.Publish(MessageBrokerChannels.AppendLog, entity);
            LogToConsole(message);
        }
    }
}