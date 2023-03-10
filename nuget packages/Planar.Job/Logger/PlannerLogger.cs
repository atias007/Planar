﻿using Microsoft.Extensions.Logging;
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

#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable IDE0060 // Remove unused parameter

        public IDisposable BeginScope<TState>(TState state) => default;

#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore CS8603 // Possible null reference return.

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

    internal class PlannerLogger<TContext> : BaseLogger, ILogger<TContext>
    {
        public PlannerLogger(MessageBroker messageBroker) : base(messageBroker)
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

    internal class PlannerLogger : BaseLogger, ILogger
    {
        public PlannerLogger(MessageBroker messageBroker) : base(messageBroker)
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