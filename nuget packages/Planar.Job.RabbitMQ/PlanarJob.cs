using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Planar.Job
{
    public static partial class PlanarJob
    {
#if NETSTANDARD2_0
        private static IConnection _connection;
        private static IChannel _channel;
        internal static string Environment { get; private set; }
#else
        private static IConnection? _connection;
        private static IChannel? _channel;
        internal static string Environment { get; private set; } = null!;
#endif
        internal static PlanarJobStartProperties Properties { get; private set; } = PlanarJobStartProperties.Default;
        internal static Stopwatch Stopwatch { get; private set; } = new Stopwatch();
        internal static RunningMode Mode { get; set; } = RunningMode.Debug;

        private static volatile bool _isConsuming;
        private static readonly SemaphoreSlim _reconnectSemaphore = new SemaphoreSlim(1, 1);

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
            Func<string, BasicDeliverEventArgs, Task> messageHandler,
            CancellationToken cancellationToken)
        {
            _isConsuming = true;

            while (_isConsuming && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await EnsureConnectionAsync(connectionFactory, queueName, cancellationToken);
#if NETSTANDARD2_0
                    var consumer = new AsyncEventingBasicConsumer(_channel);
#else
                    var consumer = new AsyncEventingBasicConsumer(_channel!);
#endif

                    consumer.ReceivedAsync += async (sender, eventArgs) =>
                    {
                        var messageBody = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
                        await messageHandler(messageBody, eventArgs);
                    };

#if NETSTANDARD2_0
                    await _channel.BasicConsumeAsync(queue: queueName, autoAck: true, consumer: consumer);
#else
                    await _channel!.BasicConsumeAsync(queue: queueName, autoAck: true, consumer: consumer);
#endif

                    // Wait until cancellation is requested
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

        private static async Task EnsureDefinition(
            IConnectionFactory connectionFactory,
            string queueName,
            CancellationToken cancellationToken)
        {
            await EnsureConnectionAsync(connectionFactory, queueName, cancellationToken);
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
        /// Ensures connection and channel are established and healthy (Singleton pattern)
        /// </summary>
        private static async Task EnsureConnectionAsync(
            IConnectionFactory connectionFactory,
            string queueName,
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

                    _connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
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

        public async static Task StartAsync<TJob>()
            where TJob : BaseJob, new()
        {
            var factory = new ConnectionFactory
            {
                HostName = "localhost",
                Port = 5672,
                UserName = "guest",
                Password = "guest",
                VirtualHost = "/",

                // Resilience settings (optional - will be configured in StartConsumeAsync if not set)
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                RequestedHeartbeat = TimeSpan.FromSeconds(60),
                RequestedConnectionTimeout = TimeSpan.FromSeconds(30)
            };

            var queueName = "Postal";
            var cancellationTokenSource = new CancellationTokenSource();

            await EnsureDefinition(
                 connectionFactory: factory,
                queueName: queueName,
                cancellationToken: cancellationTokenSource.Token);

            // Start consuming messages
            await StartConsumeAsync(
                connectionFactory: factory,
                queueName: queueName,
                messageHandler: ProcessMessageAsync,
                cancellationToken: cancellationTokenSource.Token
            );
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
    }
}