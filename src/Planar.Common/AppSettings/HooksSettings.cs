using Polly;
using System.Collections.Generic;

namespace Planar.Common
{
    public class HooksSettings
    {
        public RestHookSettings Rest { get; set; } = new();
        public TeamsHookSettings Teams { get; set; } = new();
        public TwilioSmsHookSettings TwilioSms { get; set; } = new();
        public RedisSettings Redis { get; set; } = new();
        public TelegramSettings Telegram { get; set; } = new();
        public RabbitMqSettings RabbitMq { get; set; } = new();
    }

    public class RestHookSettings
    {
        public string? DefaultUrl { get; set; }
    }

    public class TeamsHookSettings
    {
        public string? DefaultUrl { get; set; }
    }

    public class TwilioSmsHookSettings
    {
        public string? AccountSid { get; set; }
        public string? AuthToken { get; set; }
        public string? FromNumber { get; set; }
        public string? DefaultPhonePrefix { get; set; }
    }

    public class RedisSettings
    {
        public List<string> Endpoints { get; set; } = [];
        public string? Password { get; set; }
        public string? User { get; set; }
        public ushort Database { get; set; }
        public string? StreamName { get; set; }
        public string? PubSubChannel { get; set; }
        public bool Ssl { get; set; }
    }

    public class RabbitMqSettings
    {
        public string? Password { get; set; }
        public string? Username { get; set; }
        public string? VirtualHost { get; set; }
        public string? Exchange { get; set; }
        public string? RoutingKey { get; set; }
        public RabbitMqSslSettings? Ssl { get; set; }
        public IEnumerable<RabbitMqEndpoint> Endpoints { get; set; } = [];
    }

    public class RabbitMqSslSettings
    {
        public bool Enable { get; set; }
        public string PolicyErrors { get; set; } = "None";
        public string? CertPassphrase { get; set; }
        public string? CertPath { get; set; }
    }

    public class RabbitMqEndpoint
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
    }

    public class TelegramSettings
    {
        public string? BotToken { get; set; }
        public string? ChatId { get; set; }
    }
}