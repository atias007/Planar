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

            Users = JsonSerializer.Serialize(details.Users);
            Group = JsonSerializer.Serialize(details.Group);
            details.Users = null;
            details.Group = null;
            Details = JsonSerializer.Serialize(details);
        }

        public MonitorMessageBroker(ILogger<MonitorUtil> logger, MonitorSystemDetails details)
        {
            _logger = logger;

            Users = JsonSerializer.Serialize(details.Users);
            Group = JsonSerializer.Serialize(details.Group);
            details.Users = null;
            details.Group = null;
            Details = JsonSerializer.Serialize(details);
        }

        public string Users { get; set; }

        public string Group { get; set; }

        public string Details { get; set; }

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