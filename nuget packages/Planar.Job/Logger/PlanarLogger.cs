﻿using Microsoft.Extensions.Logging;
using Planar.CLI.CliGeneral;
using System;
using System.Text;

namespace Planar.Job.Logger
{
    internal class BaseLogger
    {
        private static readonly StringBuilder _logBuilder = new StringBuilder();
        private static readonly object _locker = new object();

#pragma warning disable IDE0060 // Remove unused parameter

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default;

#pragma warning restore IDE0060 // Remove unused parameter

#pragma warning disable IDE0060 // Remove unused parameter

        public bool IsEnabled(LogLevel logLevel) => true;

#pragma warning restore IDE0060 // Remove unused parameter

        public static string LogText => _logBuilder.ToString();

        protected void Log(LogLevel logLevel, string message)
        {
            var entity = new LogEntity { Message = message, Level = logLevel };
            MqttClient.PublishAsync(MessageBrokerChannels.AppendLog, entity).Wait();
            LogToConsole(entity.ToString());
        }

        protected void LogToConsole(string message)
        {
            lock (_locker)
            {
                SetLogLineColor(message);
                Console.WriteLine(message);
                Console.ResetColor();

                _logBuilder.AppendLine(message);
            }
        }

        private static void SetLogLineColor(string message)
        {
            var color = CliFormat.GetLogLineColor(message);

            Console.ForegroundColor = color switch
            {
                "red" => ConsoleColor.Red,
                "wheat1" => ConsoleColor.DarkYellow,
                "deepskyblue1" => ConsoleColor.DarkCyan,
                "magenta1" => ConsoleColor.Magenta,
                "lightsalmon1" => ConsoleColor.DarkGreen,
                _ => ConsoleColor.White,
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

            var message = $"<{typeof(TContext).Name}> {formatter(state, exception)}";
            Log(logLevel, message);

            if (exception != null)
            {
                message = $"<{typeof(TContext).Name}> {exception}";
                Log(logLevel, message);
            }
        }
    }

    internal class PlanarLogger : BaseLogger, ILogger
    {
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) { return; }

            var message = $"{formatter(state, exception)}";
            Log(logLevel, message);

            if (exception != null)
            {
                Log(logLevel, exception.ToString());
            }
        }
    }
}