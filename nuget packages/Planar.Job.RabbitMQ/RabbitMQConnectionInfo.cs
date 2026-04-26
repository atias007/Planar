using RabbitMQ.Client;
using System.Collections.Generic;

namespace Planar.Job.RabbitMQ
{
    public class RabbitMQConnectionInfo
    {
#if NETSTANDARD2_0
        public string Hostname { get; set; }
        public string VirtualHost { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public SslOption Ssl { get; set; }
#else
        public string? Hostname { get; set; }
        public string? VirtualHost { get; set; }
        public string? Username { get; set; } = string.Empty;
        public string? Password { get; set; } = string.Empty;
        public SslOption? Ssl { get; set; }
#endif

        public int? Port { get; set; }

        public List<AmqpTcpEndpoint> Endpoints { get; set; } = new List<AmqpTcpEndpoint>();
        public uint? MaxInboundMessageBodySize { get; set; }

        internal ConnectionFactory GetConnectionFactory()
        {
            var factory = new ConnectionFactory();
            if (!string.IsNullOrWhiteSpace(Hostname)) { factory.HostName = Hostname; }
            if (Port.HasValue) { factory.Port = Port.Value; }
            if (!string.IsNullOrWhiteSpace(Username)) { factory.UserName = Username; }
            if (!string.IsNullOrWhiteSpace(Password)) { factory.Password = Password; }
            if (!string.IsNullOrWhiteSpace(VirtualHost)) { factory.VirtualHost = VirtualHost; }
            if (Ssl != null) { factory.Ssl = Ssl; }
            if (MaxInboundMessageBodySize != null) { factory.MaxInboundMessageBodySize = MaxInboundMessageBodySize.Value; }
            return factory;
        }
    }
}