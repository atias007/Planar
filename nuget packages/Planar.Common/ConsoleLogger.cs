using System;

namespace Planar
{
    using Microsoft.Extensions.Logging;
    using System.Linq;

    public sealed class CustomConsoleLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly LogLevel _minLevel;

        public CustomConsoleLogger(string categoryName, LogLevel minLevel = LogLevel.Trace)
        {
            _categoryName = categoryName;
            _minLevel = minLevel;
        }

#if NETSTANDARD2_0

        public IDisposable BeginScope<TState>(TState state) => null;

#else
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
#endif

        public bool IsEnabled(LogLevel logLevel) => logLevel >= _minLevel;

#if NETSTANDARD2_0

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter)
#else
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
#endif

        {
            if (!IsEnabled(logLevel)) return;

#if NETSTANDARD2_0
            ConsoleColor color;
            switch (logLevel)
            {
                case LogLevel.Trace:
                    color = ConsoleColor.Gray;
                    break;

                case LogLevel.Debug:
                    color = ConsoleColor.Cyan;
                    break;

                case LogLevel.Information:
                    color = ConsoleColor.White;
                    break;

                case LogLevel.Warning:
                    color = ConsoleColor.Yellow;
                    break;

                case LogLevel.Error:
                    color = ConsoleColor.Red;
                    break;

                case LogLevel.Critical:
                    color = ConsoleColor.Magenta;
                    break;

                default:
                    color = ConsoleColor.White;
                    break;
            }
            ;
#else
  var color = logLevel switch
            {
                LogLevel.Trace => ConsoleColor.Gray,
                LogLevel.Debug => ConsoleColor.Cyan,
                LogLevel.Information => ConsoleColor.White,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Critical => ConsoleColor.Magenta,
                _ => ConsoleColor.White
            };
#endif

#if NETSTANDARD2_0
            string level;
            switch (logLevel)
            {
                case LogLevel.Trace:
                    level = "TRC";
                    break;

                case LogLevel.Debug:
                    level = "DBG";
                    break;

                case LogLevel.Information:
                    level = "INF";
                    break;

                case LogLevel.Warning:
                    level = "WRN";
                    break;

                case LogLevel.Error:
                    level = "ERR";
                    break;

                case LogLevel.Critical:
                    level = "CRT";
                    break;

                default:
                    level = "UNK";
                    break;
            }
            ;
#else
  var level = logLevel switch
            {
                LogLevel.Trace => "TRC",
                LogLevel.Debug => "DBG",
                LogLevel.Information => "INF",
                LogLevel.Warning => "WRN",
                LogLevel.Error => "ERR",
                LogLevel.Critical => "CRT",
                _ => "UNK"
            };
#endif

            var message = formatter(state, exception);
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var shortCategory = _categoryName.Split('.').Last(); // avoid long namespaces

            Console.ForegroundColor = color;
            Console.WriteLine($"[{timestamp}] [{level}] [{shortCategory}] {message}");

            if (!(exception is null))
            {
                Console.WriteLine($"    Exception: {exception}");
            }

            Console.ResetColor();
        }
    }
}