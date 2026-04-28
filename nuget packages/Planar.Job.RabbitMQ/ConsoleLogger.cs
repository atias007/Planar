using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Planar.Job.RabbitMQ
{
    internal class ConsoleLogger
    {
        public static async Task Log(LogLevel logLevel, string message)
        {
            var log = new LogEntity { Level = logLevel, Message = message };
            if (logLevel == LogLevel.Error || logLevel == LogLevel.Critical)
            {
                await Console.Error.WriteLineAsync(log.ToString());
            }
            else
            {
                await Console.Out.WriteLineAsync(log.ToString());
            }
        }
    }
}