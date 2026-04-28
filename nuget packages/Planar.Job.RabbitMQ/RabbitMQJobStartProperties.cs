using RabbitMQ.Client;
using System;
using System.Collections.Generic;

namespace Planar.Job.RabbitMQ
{
    public class RabbitMQJobStartProperties : PlanarJobStartProperties
    {
        public const string Exchange = "Planar";

        private readonly ConnectionFactory _factory = new ConnectionFactory();

        private string _queueName = AppDomain.CurrentDomain.FriendlyName;
        private string _planarHostName = string.Empty;

        public RabbitMQJobStartProperties(string planarHostName)
        {
            if (!string.IsNullOrWhiteSpace(planarHostName))
            {
                _planarHostName = planarHostName;
            }
        }

        private static bool IsValidBaseAddress(string baseAddress, out Uri uri)
        {
            return Uri.TryCreate(baseAddress, UriKind.Absolute, out uri);
        }

        public string ExchangeName => Exchange;

        public string PlanarHostName => _planarHostName;

        public string QueueName
        {
            get { return _queueName; }
            set
            {
                if (string.IsNullOrWhiteSpace(value)) { return; }
                _queueName = value;
            }
        }

        public string Hostname
        {
            get { return _factory.HostName; }
            set { _factory.HostName = value; }
        }

        public string UserName
        {
            get { return _factory.UserName; }
            set { _factory.UserName = value; }
        }

        public string Password
        {
            get { return _factory.Password; }
            set { _factory.Password = value; }
        }

        public string VirtualHost
        {
            get { return _factory.VirtualHost; }
            set { _factory.VirtualHost = value; }
        }

        public SslOption Ssl
        {
            get { return _factory.Ssl; }
            set { _factory.Ssl = value; }
        }

        public uint MaxInboundMessageBodySize
        {
            get { return _factory.MaxInboundMessageBodySize; }
            set { _factory.MaxInboundMessageBodySize = value; }
        }

        public int Port
        {
            get { return _factory.Port; }
            set { _factory.Port = value; }
        }

        public AmqpTcpEndpoint Endpoint
        {
            get { return _factory.Endpoint; }
            set { _factory.Endpoint = value; }
        }

        public List<AmqpTcpEndpoint> Endpoints { get; set; } = new List<AmqpTcpEndpoint>();

        internal ConnectionFactory GetConnectionFactory()
        {
            _factory.AutomaticRecoveryEnabled = true;
            _factory.NetworkRecoveryInterval = TimeSpan.FromSeconds(10);
            _factory.RequestedHeartbeat = TimeSpan.FromSeconds(60);
            _factory.RequestedConnectionTimeout = TimeSpan.FromSeconds(30);

            return _factory;
        }
    }
}