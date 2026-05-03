using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace Planar.Job.RabbitMQ
{
    internal sealed class RabbitMQFactory
    {
        private readonly SemaphoreSlim _reconnectSemaphore = new SemaphoreSlim(1, 1);
        private volatile bool _isConsuming;
        private readonly Timer _healthCheckTimer;
        private readonly IConnectionFactory connectionFactory;
        private readonly RabbitMQJobStartProperties properties;
        private readonly CancellationToken cancellationToken;
        private static readonly object _lock = new object();
        private int healthCheckCounter = 0;

#if NETSTANDARD2_0
        private IChannel _channel;
        private IConnection _connection;
        private AsyncEventingBasicConsumer _consumer;
        private static RabbitMQFactory _instance;
        private Func<BasicDeliverEventArgs, Task> _messageHandler;
#else
        private IConnection? _connection;
        private IChannel? _channel;
        private AsyncEventingBasicConsumer? _consumer;
        private static RabbitMQFactory? _instance;
        private Func<BasicDeliverEventArgs, Task>? _messageHandler;
#endif

        public static RabbitMQFactory GetInstance(RabbitMQJobStartProperties connectionInfo, CancellationToken cancellationToken)
        {
            if (_instance != null) { return _instance; }
            lock (_lock)
            {
                if (_instance != null) { return _instance; }
                _instance = new RabbitMQFactory(connectionInfo.RabbitMQConnectionFactory, connectionInfo, cancellationToken);
                return _instance;
            }
        }

        private RabbitMQFactory(IConnectionFactory connectionFactory,
            RabbitMQJobStartProperties connectionInfo,
            CancellationToken cancellationToken)
        {
            _healthCheckTimer = new Timer(20_000);
            _healthCheckTimer.Elapsed += async (sender, e) => await SafeHealthCheck();
            _healthCheckTimer.Start();
            this.connectionFactory = connectionFactory;
            this.properties = connectionInfo;
            this.cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Closes the channel and connection gracefully
        /// </summary>
        private async Task CloseConnectionAsync()
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

        private async Task SafeHealthCheck()
        {
            Interlocked.Increment(ref healthCheckCounter);

            try
            {
                _healthCheckTimer.Stop();
                await EnsureConsumer();
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
                    await EnsureDefinition();
                }
            }
            catch (Exception ex)
            {
                await ConsoleLogger.Log(LogLevel.Error, $"Failed to ensure RabbitMQ definition: {ex.Message}");
            }
        }

        private async Task EnsureConsumer()
        {
            await EnsureConnectionAsync();
            if (_channel == null) { return; }
            var total = await _channel.MessageCountAsync(properties.QueueName, cancellationToken);
            if (total == 0) { return; }
            await Task.Delay(1_000);
            total = await _channel.MessageCountAsync(properties.QueueName, cancellationToken);
            if (total == 0) { return; }

            _isConsuming = false;
            if (_messageHandler != null) { await StartConsumeAsync(_messageHandler); }
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

                    var connectionName = $"{nameof(Planar)}:{nameof(Job)}:{properties.QueueName}";

                    if (properties.RabbitMQHostNames.Count > 0)
                    {
                        _connection = await connectionFactory.CreateConnectionAsync(properties.RabbitMQHostNames, connectionName, cancellationToken);
                    }
                    else if (properties.RabbitMQEndpoints.Count > 0)
                    {
                        _connection = await connectionFactory.CreateConnectionAsync(properties.RabbitMQEndpoints, connectionName, cancellationToken);
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

        public async Task EnsureDefinition()
        {
            try
            {
                await EnsureDefinitionInner();
            }
            catch (OperationInterruptedException ex)
            {
                await ConsoleLogger.Log(LogLevel.Warning, $"Failed to declare exchange or queue. Attempting to delete and recreate. Error: {ex.Message}");
                ////await CloseConnectionAsync();
                await EnsureConnectionAsync();

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

                await EnsureDefinitionInner();
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

        private async Task EnsureDefinitionInner()
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
                    _consumer = new AsyncEventingBasicConsumer(_channel);
                    _consumer.ReceivedAsync += async (sender, eventArgs) =>
                    {
                        _ = messageHandler(eventArgs);
                    };

                    await _channel.BasicConsumeAsync(
                        queue: properties.QueueName,
                        autoAck: true,
                        consumer: _consumer);

                    await ConsoleLogger.Log(LogLevel.Information, $"Started consuming from queue '{properties.QueueName}'");

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