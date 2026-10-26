using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Planar.Common
{
    public class ConnectorsSettings
    {
        public SmtpConnectorSettings? Smtp { get; set; }
        public RestConnectorSettings? Rest { get; set; }
        public TeamsConnectorSettings? Teams { get; set; }
        public TwilioSmsConnectorSettings? TwilioSms { get; set; }
        public RedisConnectorSettings? Redis { get; set; }
        public TelegramConnectorSettings? Telegram { get; set; }
        public RabbitMqConnectorSettings? RabbitMq { get; set; }
    }

    public enum ImageMode
    {
        Embedded,
        Internal,
        External
    }

    public class SmtpConnectorSettings(string host)
    {
        public string? FromAddress { get; set; }
        public string? FromName { get; set; }
        public string Host { get; set; } = host;
        public int Port { get; set; }
        public bool UseSsl { get; set; }
        public bool UseDefaultCredentials { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public ImageMode HtmlImageMode { get; set; }
        public string? HtmlImageInternalBaseUrl { get; set; }

        public static SmtpConnectorSettings? GetInstance(string host)
        {
            if (string.IsNullOrWhiteSpace(host)) { return null; }
            return new SmtpConnectorSettings(host);
        }
    }

    public class RestConnectorSettings
    {
        private RestConnectorSettings(string defaultUrl)
        {
            DefaultUrl = defaultUrl;
        }

        public string DefaultUrl { get; set; }

        public static RestConnectorSettings? GetInstance(string defaultUrl)
        {
            if (string.IsNullOrWhiteSpace(defaultUrl)) { return null; }
            return new RestConnectorSettings(defaultUrl);
        }
    }

    public class TeamsConnectorSettings
    {
        private TeamsConnectorSettings(string defaultUrl)
        {
            DefaultUrl = defaultUrl;
        }

        public string DefaultUrl { get; private set; }

        public static TeamsConnectorSettings? GetInstance(string defaultUrl)
        {
            if (string.IsNullOrWhiteSpace(defaultUrl)) { return null; }
            return new TeamsConnectorSettings(defaultUrl);
        }
    }

    public class TwilioSmsConnectorSettings
    {
        private TwilioSmsConnectorSettings(string accountSid, string authToken, string fromNumber)
        {
            AccountSid = accountSid;
            AuthToken = authToken;
            FromNumber = fromNumber;
        }

        public string AccountSid { get; private set; }
        public string AuthToken { get; private set; }
        public string FromNumber { get; private set; }
        public string? DefaultPhonePrefix { get; set; }

        public static TwilioSmsConnectorSettings? GetInstance(string accountSid, string authToken, string fromNumber)
        {
            if (
                string.IsNullOrWhiteSpace(accountSid) &&
                string.IsNullOrWhiteSpace(authToken) &&
                string.IsNullOrWhiteSpace(fromNumber)) { return null; }

            return new TwilioSmsConnectorSettings(accountSid, authToken, fromNumber);
        }
    }

    public class RedisConnectorSettings
    {
        private RedisConnectorSettings(ReadOnlyCollection<string> endpoints)
        {
            Endpoints = endpoints;
        }

        public ReadOnlyCollection<string> Endpoints { get; }
        public string? Password { get; set; }
        public string? User { get; set; }
        public ushort Database { get; set; } = 0;
        public string? ServiceName { get; set; }
        public string? StreamName { get; set; }
        public int StreamSize { get; set; } = 1000;
        public string? PubSubChannel { get; set; }
        public bool Ssl { get; set; }

        public static RedisConnectorSettings? GetInstance(ReadOnlyCollection<string> endpoints)
        {
            if (endpoints == null || endpoints.Count == 0) { return null; }
            return new RedisConnectorSettings(endpoints);
        }
    }

    public class RabbitMqConnectorSettings
    {
        private RabbitMqConnectorSettings(
            IReadOnlyCollection<RabbitMqEndpoint> endpoints,
            string username,
            string password)
        {
            Endpoints = endpoints;
            Username = username;
            Password = password;
        }

        public string Password { get; set; }
        public string Username { get; set; }
        public string? VirtualHost { get; set; }
        public string? Exchange { get; set; }
        public string? RoutingKey { get; set; }
        public RabbitMqSslSettings? Ssl { get; set; }
        public IReadOnlyCollection<RabbitMqEndpoint> Endpoints { get; }

        public static RabbitMqConnectorSettings? GetInstance(IReadOnlyCollection<RabbitMqEndpoint> endpoints, string username, string password)
        {
            if (endpoints == null || endpoints.Count == 0) { return null; }
            return new RabbitMqConnectorSettings(endpoints, username, password);
        }
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

    public class TelegramConnectorSettings
    {
        private TelegramConnectorSettings(string botToken, string chatId)
        {
            BotToken = botToken;
            ChatId = chatId;
        }

        public string BotToken { get; }
        public string ChatId { get; }

        public static TelegramConnectorSettings? GetInstance(string botToken, string chatId)
        {
            if (string.IsNullOrWhiteSpace(botToken) && string.IsNullOrWhiteSpace(chatId)) { return null; }
            return new TelegramConnectorSettings(botToken, chatId);
        }
    }
}