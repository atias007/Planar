using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Planar.Job.RabbitMQ;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Job
{
    public static partial class PlanarJob
    {
        private static readonly SemaphoreSlim _reconnectSemaphore = new SemaphoreSlim(1, 1);
        private static volatile bool _isConsuming;

#if NETSTANDARD2_0
        private static IChannel _channel;
        private static IConnection _connection;
        internal static string Environment { get; private set; }
#else
        private static IConnection? _connection;
        private static IChannel? _channel;
        internal static string Environment { get; private set; } = null!;
#endif
        internal static RunningMode Mode { get; set; } = RunningMode.Debug;
        internal static PlanarJobStartProperties Properties { get; private set; } = PlanarJobStartProperties.Default;
        internal static Stopwatch Stopwatch { get; private set; } = new Stopwatch();

        public async static Task StartAsync<TJob>(RabbitMQConnectionInfo connectionInfo)
                    where TJob : BaseJob, new()
        {
            var factory = connectionInfo.GetConnectionFactory();
            factory.AutomaticRecoveryEnabled = true;
            factory.NetworkRecoveryInterval = TimeSpan.FromSeconds(10);
            factory.RequestedHeartbeat = TimeSpan.FromSeconds(60);
            factory.RequestedConnectionTimeout = TimeSpan.FromSeconds(30);

            var queueName = "Postal";

            var host = Host.CreateDefaultBuilder(System.Environment.GetCommandLineArgs()).Build();
            var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
            var cancellationToken = lifetime.ApplicationStopping;

            await EnsureDefinition(
                connectionFactory: factory,
                queueName,
                connectionInfo,
                cancellationToken: cancellationToken);

            // Start consuming messages
            await StartConsumeAsync(
                connectionFactory: factory,
                queueName,
                connectionInfo,
                messageHandler: ProcessMessageAsync,
                cancellationToken: cancellationToken
            );
        }

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
            string queueName,
            RabbitMQConnectionInfo connectionInfo,
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

                    var connectionName = $"{nameof(Planar)}:{nameof(Job)}:{queueName}";

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

                    // Declare queue with durability for resilience
                    await _channel.QueueDeclareAsync(
                        queue: queueName,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null,
                        cancellationToken: cancellationToken);
                }
            }
            finally
            {
                _reconnectSemaphore.Release();
            }
        }

        private static async Task EnsureDefinition(
                    IConnectionFactory connectionFactory,
                    string queueName,
                    RabbitMQConnectionInfo connectionInfo,
                    CancellationToken cancellationToken)
        {
            await EnsureConnectionAsync(connectionFactory, queueName, connectionInfo, cancellationToken);
            if (_channel == null) { return; }

            await _channel.ExchangeDeclareAsync(
                exchange: "Planar",
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false,
                arguments: null);

            await _channel.QueueDeclareAsync(
                queue: "Planar.task1",
                durable: true,        // quorum queues must be durable
                exclusive: false,     // quorum queues cannot be exclusive
                autoDelete: false);    // quorum queues cannot be auto-delete

            await _channel.QueueBindAsync(
                queue: "Planar.task1",
                exchange: "Planar",
                routingKey: "task1",
                arguments: null);

            await Console.Out.WriteLineAsync("Exchange, queue, and binding created successfully.");
        }

        /// <summary>
        /// Message handler - processes each received message
        /// </summary>
        /// <param name="messageBody">The message body as a string</param>
        /// <param name="eventArgs">Event arguments containing message metadata</param>
        /// <returns>True if message processed successfully, False to requeue</returns>
        private static async Task ProcessMessageAsync(string messageBody, BasicDeliverEventArgs eventArgs)
        {
            try
            {
                Console.WriteLine($"Received message: {messageBody}");
                Console.WriteLine($"Delivery Tag: {eventArgs.DeliveryTag}");
                Console.WriteLine($"Routing Key: {eventArgs.RoutingKey}");

                // Simulate processing
                await Task.Delay(100);

                // Your business logic here
                // ...
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing message: {ex.Message}");
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
        private static async Task StartConsumeAsync(
            IConnectionFactory connectionFactory,
            string queueName,
            RabbitMQConnectionInfo connectionInfo,
            Func<string, BasicDeliverEventArgs, Task> messageHandler,
            CancellationToken cancellationToken)
        {
            _isConsuming = true;

            while (_isConsuming && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await EnsureConnectionAsync(connectionFactory, queueName, connectionInfo, cancellationToken);
                    if (_channel == null) { continue; }
                    var consumer = new AsyncEventingBasicConsumer(_channel);

                    consumer.ReceivedAsync += async (sender, eventArgs) =>
                    {
                        var messageBody = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
                        await messageHandler(messageBody, eventArgs);
                    };

                    await _channel.BasicConsumeAsync(queue: queueName, autoAck: true, consumer: consumer);
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