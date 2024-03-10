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
    }

    public class RestHookSettings
    {
        public string? DefaultUrl { get; set; }
    }

    public class TeamsHookSettings
    {
        public string? DefaultUrl { get; set; }
        public bool SendToMultipleChannels { get; set; }
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

    public class TelegramSettings
    {
        public string? BotToken { get; set; }
        public string? ChatId { get; set; }
    }
}