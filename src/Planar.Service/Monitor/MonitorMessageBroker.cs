using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;

namespace Planar.Service.Monitor
{
    public class MonitorMessageBroker
    {
        private readonly ILogger<MonitorUtil> _logger;

        public MonitorMessageBroker(ILogger<MonitorUtil> logger, MonitorDetails details)
        {
            _logger = logger;
            HandleMonitor(details);
        }

        public MonitorMessageBroker(ILogger<MonitorUtil> logger, MonitorSystemDetails details)
        {
            _logger = logger;
            HandleMonitor(details);
        }

        public void HandleMonitor<T>(T monitor)
            where T : Monitor
        {
            Users = JsonSerializer.Serialize(monitor.Users);
            Group = JsonSerializer.Serialize(monitor.Group);
            GlobalConfig = JsonSerializer.Serialize(monitor.GlobalConfig);

            monitor.Users = null;
            monitor.Group = null;
            monitor.GlobalConfig = null;

            Details = JsonSerializer.Serialize(monitor);
        }

        public string SpecVersion => "1.0";

        public string Users { get; set; } = null!;

        public string Group { get; set; } = null!;

        public string Details { get; set; } = null!;

        public string GlobalConfig { get; set; } = null!;

        public string? Exception { get; set; }

        public string? MostInnerException { get; set; }

        public string? MostInnerExceptionMessage { get; set; }

        public void LogError(Exception exception, string message, params object[] args)
        {
            if (exception == null)
            {
#pragma warning disable CA2254 // Template should be a static expression
                _logger.LogError(message, args);
            }
            else
            {
                _logger.LogError(exception, message, args);
#pragma warning restore CA2254 // Template should be a static expression
            }
        }
    }
}