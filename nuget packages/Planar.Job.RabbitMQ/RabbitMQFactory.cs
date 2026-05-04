using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace Planar.Job.RabbitMq
{
    internal sealed class RabbitMqFactory
    {
        private bool _isConsuming = false;
        private readonly SemaphoreSlim _reconnectSemaphore = new SemaphoreSlim(1, 1);
        private readonly Timer _healthCheckTimer;
        private readonly IConnectionFactory connectionFactory;
        private readonly RabbitMqJobStartProperties properties;
        private readonly CancellationToken cancellationToken;
        private readonly Dictionary<string, AsyncEventingBasicConsumer> _consumers = new Dictionary<string, AsyncEventingBasicConsumer>();
        private static readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private int healthCheckCounter = 0;

#if NETSTANDARD2_0
        private IChannel _channel;
        private IConnection _connection;
        private static RabbitMqFactory _instance;
        private Func<BasicDeliverEventArgs, Task> _messageHandler;
#else
        private IConnection? _connection;
        private IChannel? _channel;
        private static RabbitMqFactory? _instance;
        private Func<BasicDeliverEventArgs, Task>? _messageHandler;
#endif

        public static async Task<RabbitMqFactory> GetInstance(RabbitMqJobStartProperties properties, CancellationToken cancellationToken)
        {
            if (_instance != null) { return _instance; }
            await _lock.WaitAsync(cancellationToken);
            try
            {
                if (_instance != null) { return _instance; }
                _instance = new RabbitMqFactory(properties.RabbitMQConnectionFactory, properties, cancellationToken);
                await _instance.EnsureDefinitions();
                return _instance;
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task EnsureDefinitions()
        {
            foreach (var def in properties.JobDefinitions)
            {
                await EnsureDefinition(queueName: def.QueueName);
            }
        }

        private async Task EnsureDefinition(string queueName)
        {
            try
            {
                await EnsureDefinitionInner(queueName);
            }
            catch (OperationInterruptedException ex)
            {
                await ConsoleLogger.Log(LogLevel.Warning, $"Failed to declare exchange or queue. Attempting to delete and recreate. Error: {ex.Message}");
                await EnsureConnectionAsync();

                if (_channel == null) { return; }
                await _channel.QueueDeleteAsync(
                    queueName,
                    ifUnused: false,
                    ifEmpty: false,
                    cancellationToken: cancellationToken);

                await _channel.QueueUnbindAsync(
                    queueName,
                    properties.ExchangeName,
                    routingKey: queueName,
                    arguments: null,
                    cancellationToken);

                await EnsureDefinitionInner(queueName);
            }
        }

        /// <summary>
        /// Starts consuming messages from RabbitMQ queue with automatic reconnection and resilience
        /// </summary>
        /// <param name="connectionFactory">RabbitMQ connection factory with configured settings</param>
        /// <param name="queueName">Name of the queue to consume from</param>
        /// <param name="messageHandler">Handler function to process received messages. Returns true if successful, false to requeue.</param>
        /// <param name="cancellationToken">Cancellation token to stop consuming</param>
        /// <returns>Task representing the consumer operation</returns>
        public async Task StartConsumeAsync(Func<BasicDeliverEventArgs, Task> messageHandler)
        {
            _isConsuming = true;
            _messageHandler = messageHandler;

            while (_isConsuming && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await EnsureConnectionAsync();
                    if (_channel == null) { continue; }

                    foreach (var def in properties.JobDefinitions)
                    {
                        var consumer = await CreateConsumer(def.QueueName);
                        _consumers.Add(def.QueueName, consumer);
                    }

                    var names = string.Join(",", properties.JobDefinitions.Select(d => d.QueueName));
                    await ConsoleLogger.Log(LogLevel.Information, $"Started consuming from queue(s) '{names}'");
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

        private async Task<AsyncEventingBasicConsumer> CreateConsumer(string queueName)
        {
            if (_channel == null) { throw new InvalidOperationException("Channel is not established"); }
            if (_messageHandler == null) { throw new InvalidOperationException("Message handler is not set"); }

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (sender, eventArgs) =>
            {
                _ = _messageHandler(eventArgs);
            };

            await _channel.BasicConsumeAsync(
                queue: queueName,
                autoAck: true,
                consumerTag: $"Planar:{queueName}",
                consumer: consumer);

            return consumer;
        }

        private RabbitMqFactory(IConnectionFactory connectionFactory,
            RabbitMqJobStartProperties properties,
            CancellationToken cancellationToken)
        {
            _healthCheckTimer = new Timer(20_000);
            _healthCheckTimer.Elapsed += async (sender, e) => await SafeHealthCheck();
            _healthCheckTimer.Start();
            this.connectionFactory = connectionFactory;
            this.properties = properties;
            this.cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Closes the channel and connection gracefully
        /// </summary>
        private async Task CloseConnectionAsync()
        {
            try
            {
                if (_channel != null) { await _channel.CloseAsync(); }
                _channel?.Dispose();
                _channel = null;
            }
            catch { }

            try
            {
                if (_connection != null) { await _connection.CloseAsync(); }
                _connection?.Dispose();
                _connection = null;
            }
            catch { }
        }

        private async Task SafeHealthCheck()
        {
            Interlocked.Increment(ref healthCheckCounter);

            try
            {
                _healthCheckTimer.Stop();
                await EnsureConsumers();
            }
            catch (Exception ex)
            {
                await ConsoleLogger.Log(LogLevel.Error, $"Failed to ensure that RabbitMQ connection is alive: {ex.Message}");
            }
            finally
            {
                _healthCheckTimer.Start();
            }

            try
            {
                if (healthCheckCounter == 1_000_000_000) { healthCheckCounter = 0; }
                if (healthCheckCounter % 30 == 0)
                {
                    await EnsureDefinitions();
                }
            }
            catch (Exception ex)
            {
                await ConsoleLogger.Log(LogLevel.Error, $"Failed to ensure RabbitMQ definition: {ex.Message}");
            }
        }

        private async Task EnsureConsumers()
        {
            await EnsureConnectionAsync();

            foreach (var consumer in _consumers)
            {
                await EnsureConsumer(consumer.Key);
            }
        }

        private async Task EnsureConsumer(string queueName)
        {
            if (_channel == null) { return; }

            var total = await _channel.MessageCountAsync(queueName, cancellationToken);
            if (total == 0) { return; }
            await Task.Delay(1_000);
            total = await _channel.MessageCountAsync(queueName, cancellationToken);
            if (total == 0) { return; }

            var consumer = await CreateConsumer(queueName);
            _consumers[queueName] = consumer;
        }

        /// <summary>
        /// Ensures connection and channel are established and healthy (Singleton pattern)
        /// </summary>
        private async Task EnsureConnectionAsync()
        {
            await _reconnectSemaphore.WaitAsync(cancellationToken);

            try
            {
                // Ensure connection is open
                if (_connection == null || !_connection.IsOpen)
                {
                    if (_connection != null)
                    {
                        await _connection.CloseAsync(cancellationToken);
                        _connection = null;
                    }

                    var connectionName = $"{nameof(Planar)}:{nameof(Job)}:{nameof(RabbitMQ)}";

                    if (properties.RabbitMqEndpoints.Any())
                    {
                        _connection = await connectionFactory.CreateConnectionAsync(properties.RabbitMqEndpoints, connectionName, cancellationToken);
                    }
                    else
                    {
                        _connection = await connectionFactory.CreateConnectionAsync(connectionName, cancellationToken);
                    }

                    _connection.ConnectionShutdownAsync += async (sender, args) =>
                    {
                        await ConsoleLogger.Log(LogLevel.Warning, $"RabbitMQ connection shutdown: {args.ReplyText}");
                        await CloseConnectionAsync();
                    };

                    _connection.ConnectionRecoveryErrorAsync += async (sender, args) =>
                    {
                        await ConsoleLogger.Log(LogLevel.Error, $"RabbitMQ connection recovery error: {args.Exception.Message}");
                        await CloseConnectionAsync();
                    };

                    _connection.RecoverySucceededAsync += async (sender, args) =>
                    {
                        await ConsoleLogger.Log(LogLevel.Information, $"RabbitMQ connection recovery succeeded");
                    };
                }

                // Ensure channel is open
                if (_channel == null || !_channel.IsOpen)
                {
                    if (_channel != null)
                    {
                        await _channel.DisposeAsync();
                        await SafeInvoke(() => _channel.DisposeAsync());
                        _channel = null;
                    }

                    _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
                    // Set Quality of Service (prefetch)
                    await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 100, global: false, cancellationToken: cancellationToken);
                    await ConsoleLogger.Log(LogLevel.Information, $"Connected to RabbitMQ");
                }
            }
            finally
            {
                _reconnectSemaphore.Release();
            }
        }

        private static async ValueTask SafeInvoke(Func<ValueTask> func)
        {
            try
            {
                await func();
            }
            catch
            {
                /// *** DO NOTHING *** ///
            }
        }

        private async Task EnsureDefinitionInner(string queueName)
        {
            await EnsureConnectionAsync();
            if (_channel == null) { return; }

            await _channel.ExchangeDeclareAsync(
                exchange: properties.ExchangeName,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false,
                arguments: null);

            await _channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,        // quorum queues must be durable
                exclusive: false,     // quorum queues cannot be exclusive
                autoDelete: false);   // quorum queues cannot be auto-delete

            await _channel.QueueBindAsync(
                queue: queueName,
                exchange: properties.ExchangeName,
                routingKey: queueName,
                arguments: null);

            await ConsoleLogger.Log(LogLevel.Information, $"Exchange '{properties.ExchangeName}' and queue '{queueName}' created successfully");
        }
    }
}