using Microsoft.Extensions.Logging;
using System;

namespace Planar
{
    public class LogEntity
    {
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
}