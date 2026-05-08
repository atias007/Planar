using Planar.Common;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Quartz.Logging.OperationName;
using Timer = System.Timers.Timer;

namespace PlanarJob
{
    internal sealed class RabbitMqFactory
    {
        private readonly SemaphoreSlim _reconnectSemaphore = new(1, 1);
        private readonly Timer _healthCheckTimer;
        private readonly ConnectionFactory _connectionFactory;
        private readonly CancellationToken _cancellationToken;
        private readonly IEnumerable<AmqpTcpEndpoint> _endpoints;
        private static readonly SemaphoreSlim _lock = new(1, 1);
        private int healthCheckCounter = 0;

        private IConnection? _connection;
        private IChannel? _channel;
        private static RabbitMqFactory? _instance;

        public static async Task<RabbitMqFactory> GetInstance(CancellationToken cancellationToken)
        {
            if (_instance != null) { return _instance; }
            await _lock.WaitAsync(cancellationToken);
            try
            {
                if (_instance != null) { return _instance; }
                _instance = new RabbitMqFactory(cancellationToken);
                return _instance;
            }
            finally
            {
                _lock.Release();
            }
        }

        private RabbitMqFactory(CancellationToken cancellationToken)
        {
            _healthCheckTimer = new Timer(20_000);

            var settings = AppSettings.Hooks.RabbitMq;

            if (settings.Endpoints == null || !settings.Endpoints.Any())
            {
                _connectionFactory = new ConnectionFactory();
                _endpoints = [];
                return;
            }

            _connectionFactory = new ConnectionFactory();
            if (!string.IsNullOrEmpty(settings.Username)) { _connectionFactory.UserName = settings.Username; }
            if (!string.IsNullOrEmpty(settings.Password)) { _connectionFactory.Password = settings.Password; }
            if (!string.IsNullOrEmpty(settings.VirtualHost)) { _connectionFactory.VirtualHost = settings.VirtualHost; }

            _endpoints = settings.Endpoints.Select(e => new AmqpTcpEndpoint(e.Host, e.Port));

            if (settings.Ssl != null)
            {
                _connectionFactory.Ssl = new SslOption
                {
                    Enabled = settings.Ssl.Enable,
                    ServerName = settings.Ssl.SslPolicyErrors,
                    CertPassphrase = settings.Ssl.CertPassphrase
                };
            }

            _healthCheckTimer.Elapsed += async (sender, e) => await SafeHealthCheck();
            _healthCheckTimer.Start();
            _cancellationToken = cancellationToken;
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
            catch
            {
                // DO NOTHING //
            }

            try
            {
                if (_connection != null) { await _connection.CloseAsync(); }
                _connection?.Dispose();
                _connection = null;
            }
            catch
            {
                // DO NOTHING //
            }
        }

        private async Task SafeHealthCheck()
        {
            Interlocked.Increment(ref healthCheckCounter);

            try
            {
                _healthCheckTimer.Stop();
                await EnsureConnectionAsync();
            }
            catch (Exception ex)
            {
                // TODO: log the error
            }
            finally
            {
                _healthCheckTimer.Start();
            }
        }

        /// <summary>
        /// Ensures connection and channel are established and healthy (Singleton pattern)
        /// </summary>
        private async Task EnsureConnectionAsync()
        {
            if (!_endpoints.Any())
            {
                throw new InvalidOperationException("No RabbitMQ endpoints configured");
            }

            await _reconnectSemaphore.WaitAsync(_cancellationToken);

            try
            {
                // Ensure connection is open
                if (_connection == null || !_connection.IsOpen)
                {
                    if (_connection != null)
                    {
                        await SafeInvoke(() => _connection.CloseAsync(_cancellationToken));
                        _connection = null;
                    }

                    var connectionName = $"{nameof(Planar)}:Server:{Environment.MachineName}";
                    _connection = await _connectionFactory.CreateConnectionAsync(_endpoints, connectionName, _cancellationToken);
                    _connection.ConnectionShutdownAsync += async (sender, args) =>
                    {
                        // TODO: log
                        //// await ConsoleLogger.Log(LogLevel.Warning, $"RabbitMQ connection shutdown: {args.ReplyText}");
                        await CloseConnectionAsync();
                    };

                    _connection.ConnectionRecoveryErrorAsync += async (sender, args) =>
                    {
                        // TODO: log
                        //// await ConsoleLogger.Log(LogLevel.Error, $"RabbitMQ connection recovery error: {args.Exception.Message}");
                        await CloseConnectionAsync();
                    };

                    _connection.RecoverySucceededAsync += async (sender, args) =>
                    {
                        // TODO: log
                        //// await ConsoleLogger.Log(LogLevel.Information, $"RabbitMQ connection recovery succeeded");
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

                    _channel = await _connection.CreateChannelAsync(cancellationToken: _cancellationToken);
                    // Set Quality of Service (prefetch)
                    await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 100, global: false, cancellationToken: _cancellationToken);
                    // TODO: log
                    //// await ConsoleLogger.Log(LogLevel.Information, $"Connected to RabbitMQ");
                }
            }
            finally
            {
                _reconnectSemaphore.Release();
            }
        }

        private static async Task SafeInvoke(Func<Task> func)
        {
            try
            {
                await func();
            }
            catch
            {
                // *** DO NOTHING *** //
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
                // *** DO NOTHING *** //
            }
        }
    }
}