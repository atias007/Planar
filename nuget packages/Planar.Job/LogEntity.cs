using Microsoft.Extensions.Logging;
using System;

namespace Planar
{
    internal class LogEntity
    {
        // ************************** BE AWARE TO SYNC WITH PLANAR SOLUTION **************************

        public LogEntity()
        {
        }

        public LogEntity(LogLevel level, string message)
        {
            Level = level;
            Message = message;
        }

        public string Message { get; set; } = string.Empty;

        public LogLevel Level { get; set; }

        public override string ToString()
        {
            var formatedMessage = $"[{DateTime.Now:HH:mm:ss} {GetLogLevelDisplayTest(Level)}] {Message}";
            return formatedMessage;
        }

        private static string GetLogLevelDisplayTest(LogLevel logLevel)
        {
#if NETSTANDARD2_0

            switch (logLevel)
            {
                case LogLevel.Trace:
                    return "TRC";

                case LogLevel.Debug:
                    return "DBG";

                case LogLevel.Information:
                    return "INF";

                case LogLevel.Warning:
                    return "WRN";

                case LogLevel.Error:
                    return "ERR";

                case LogLevel.Critical:
                    return "CRT";

                case LogLevel.None: // Explicitly handle LogLevel.None
                default: // Optional default case, but good practice to include
                    return "NON";
            }
        }

#else

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
#endif

        // ************************** BE AWARE TO SYNC WITH PLANAR SOLUTION **************************
    }
}