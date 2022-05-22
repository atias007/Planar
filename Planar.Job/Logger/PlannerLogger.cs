using Microsoft.Extensions.Logging;
using System;

namespace Planar.Job.Logger
{
    internal class PlannerLogger : ILogger
    {
        private readonly MessageBroker _messageBroker;

        public PlannerLogger(MessageBroker messageBroker)
        {
            _messageBroker = messageBroker;
        }

        public IDisposable BeginScope<TState>(TState state) => default;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel)) { return; }

            var message = $"[{logLevel}] {formatter(state, exception)}";
            _messageBroker.Publish(MessageBrokerChannels.AppendInformation, message);

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Out.WriteLineAsync(message);
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}