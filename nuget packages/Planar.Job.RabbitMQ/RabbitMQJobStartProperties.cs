using RabbitMQ.Client;
using System;
using System.Collections.Generic;

namespace Planar.Job.RabbitMQ
{
    public class RabbitMQJobStartProperties : PlanarJobStartProperties
    {
        public const string Exchange = "Planar";
        public ConnectionFactory RabbitMQConnectionFactory = new ConnectionFactory();

        private readonly string _planarHostName = "127.0.0.1";
        private string _queueName = AppDomain.CurrentDomain.FriendlyName;

        public RabbitMQJobStartProperties(string planarHostName)
        {
            if (!string.IsNullOrWhiteSpace(planarHostName))
            {
                _planarHostName = planarHostName;
            }
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

        public List<AmqpTcpEndpoint> RabbitMQEndpoints { get; private set; } = new List<AmqpTcpEndpoint>();
        public List<string> RabbitMQHostNames { get; private set; } = new List<string>();
    }
}