using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Job.RabbitMQ
{
    internal sealed class RabbitMQFactory
    {
        private static readonly SemaphoreSlim _reconnectSemaphore = new SemaphoreSlim(1, 1);
        private static volatile bool _isConsuming;

#if NETSTANDARD2_0
        private static IChannel _channel;
        private static IConnection _connection;
#else
        private static IConnection? _connection;
        private static IChannel? _channel;
#endif

        /// <summary>
        /// Closes the channel and connection gracefully
        /// </summary>
        private static async Task CloseConnectionAsync()
        {
            try
            {
                if (_channel != null)
                {
                    await _channel.CloseAsync();
                    _channel.Dispose();
                    _channel = null;
                }
            }
            catch { }

            try
            {
                if (_connection != null)
                {
                    await _connection.CloseAsync();
                    _connection.Dispose();
                    _connection = null;
                }
            }
            catch { }
        }

        /// <summary>
        /// Ensures connection and channel are established and healthy (Singleton pattern)
        /// </summary>
        private static async Task EnsureConnectionAsync(
            IConnectionFactory connectionFactory,
            RabbitMQJobStartProperties connectionInfo,
            CancellationToken cancellationToken)
        {
            await _reconnectSemaphore.WaitAsync(cancellationToken);

            try
            {
                // Ensure connection is open
                if (_connection == null || !_connection.IsOpen)
                {
                    if (_connection != null)
                    {
                        await _connection.CloseAsync();
                        _connection = null;
                    }

                    var connectionName = $"{nameof(Planar)}:{nameof(Job)}:{connectionInfo.QueueName}";

                    if (connectionInfo.Endpoints.Count > 0)
                    {
                        _connection = await connectionFactory.CreateConnectionAsync(connectionInfo.Endpoints, connectionName, cancellationToken);
                    }
                    else
                    {
                        _connection = await connectionFactory.CreateConnectionAsync(connectionName, cancellationToken);
                    }
                }

                // Ensure channel is open
                if (_channel == null || !_channel.IsOpen)
                {
                    if (_channel != null)
                    {
                        await _channel.CloseAsync();
                        _channel = null;
                    }

                    _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
                    // Set Quality of Service (prefetch)
                    await _channel.BasicQosAsync(0, prefetchCount: 1, false, cancellationToken);
                    await ConsoleLogger.Log(LogLevel.Information, $"Connected to RabbitMQ");
                }
            }
            finally
            {
                _reconnectSemaphore.Release();
            }
        }

        public static async Task EnsureDefinition(
                    IConnectionFactory connectionFactory,
                    RabbitMQJobStartProperties properties,
                    CancellationToken cancellationToken)
        {
            try
            {
                await EnsureDefinitionInner(connectionFactory, properties, cancellationToken);
            }
            catch (OperationInterruptedException ex)
            {
                await ConsoleLogger.Log(LogLevel.Warning, $"Failed to declare exchange or queue. Attempting to delete and recreate. Error: {ex.Message}");
                ////await CloseConnectionAsync();
                await EnsureConnectionAsync(connectionFactory, properties, cancellationToken);

                if (_channel == null) { return; }
                await _channel.QueueDeleteAsync(
                    properties.QueueName,
                    ifUnused: false,
                    ifEmpty: false,
                    cancellationToken: cancellationToken);

                await _channel.QueueUnbindAsync(
                    properties.QueueName,
                    properties.ExchangeName,
                    routingKey: properties.QueueName,
                    arguments: null,
                    cancellationToken);

                await EnsureDefinitionInner(connectionFactory, properties, cancellationToken);
            }
        }

        private static async Task EnsureDefinitionInner(
                    IConnectionFactory connectionFactory,
                    RabbitMQJobStartProperties properties,
                    CancellationToken cancellationToken)
        {
            await EnsureConnectionAsync(connectionFactory, properties, cancellationToken);
            if (_channel == null) { return; }

            await _channel.ExchangeDeclareAsync(
                exchange: properties.ExchangeName,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false,
                arguments: null);

            await _channel.QueueDeclareAsync(
                queue: properties.QueueName,
                durable: true,        // quorum queues must be durable
                exclusive: false,     // quorum queues cannot be exclusive
                autoDelete: false);   // quorum queues cannot be auto-delete

            await _channel.QueueBindAsync(
                queue: properties.QueueName,
                exchange: properties.ExchangeName,
                routingKey: properties.QueueName,
                arguments: null);

            await ConsoleLogger.Log(LogLevel.Information, $"Exchange '{properties.ExchangeName}' and queue '{properties.QueueName}' created successfully");
        }

        /// <summary>
        /// Starts consuming messages from RabbitMQ queue with automatic reconnection and resilience
        /// </summary>
        /// <param name="connectionFactory">RabbitMQ connection factory with configured settings</param>
        /// <param name="queueName">Name of the queue to consume from</param>
        /// <param name="messageHandler">Handler function to process received messages. Returns true if successful, false to requeue.</param>
        /// <param name="cancellationToken">Cancellation token to stop consuming</param>
        /// <returns>Task representing the consumer operation</returns>
        public static async Task StartConsumeAsync(
            IConnectionFactory connectionFactory,
            RabbitMQJobStartProperties connectionInfo,
            Func<BasicDeliverEventArgs, Task> messageHandler,
            CancellationToken cancellationToken)
        {
            _isConsuming = true;

            while (_isConsuming && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await EnsureConnectionAsync(connectionFactory, connectionInfo, cancellationToken);
                    if (_channel == null) { continue; }
                    var consumer = new AsyncEventingBasicConsumer(_channel);

                    consumer.ReceivedAsync += async (sender, eventArgs) =>
                    {
                        await messageHandler(eventArgs);
                    };

                    await _channel.BasicConsumeAsync(
                        queue: connectionInfo.QueueName,
                        autoAck: true,
                        consumer: consumer);

                    await ConsoleLogger.Log(LogLevel.Information, $"Started consuming from queue '{connectionInfo.QueueName}'");

                    await Task.Delay(Timeout.Infinite, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _isConsuming = false;
                    break;
                }
                catch (Exception)
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        // Wait before attempting to reconnect
                        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                    }
                }
            }

            await CloseConnectionAsync();
        }
    }
}