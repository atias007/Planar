using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Planar.Common;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace PlanarJob;

public sealed class RabbitMqFactory
{
    private readonly CancellationToken _cancellationToken;
    private readonly ConnectionFactory _connectionFactory;
    private readonly IEnumerable<AmqpTcpEndpoint> _endpoints;
    private readonly Timer _healthCheckTimer;
    private readonly ILogger<RabbitMqFactory> _logger;
    private readonly SemaphoreSlim _reconnectSemaphore = new(1, 1);
    private IConnection? _connection;
    private int healthCheckFailCounter = 0;

    public RabbitMqFactory(IHostApplicationLifetime hostApplicationLifetime, ILogger<RabbitMqFactory> logger)
    {
        _logger = logger;
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

        if (settings.Ssl?.Enable ?? false)
        {
            _connectionFactory.Ssl = new SslOption
            {
                Enabled = settings.Ssl.Enable,
                AcceptablePolicyErrors = Enum.Parse<SslPolicyErrors>(settings.Ssl.PolicyErrors, ignoreCase: true),
                CertPassphrase = settings.Ssl.CertPassphrase,
            };

            if (!string.IsNullOrWhiteSpace(settings.Ssl.CertPath))
            {
                _connectionFactory.Ssl.CertPath = settings.Ssl.CertPath;
            }
        }

        _endpoints = settings.Endpoints.Select(e => new AmqpTcpEndpoint(e.Host, e.Port));

        if (settings.Ssl != null)
        {
            _connectionFactory.Ssl = new SslOption
            {
                Enabled = settings.Ssl.Enable,
                ServerName = settings.Ssl.PolicyErrors,
                CertPassphrase = settings.Ssl.CertPassphrase
            };
        }

        _healthCheckTimer.Elapsed += async (sender, e) => await SafeHealthCheck();
        _healthCheckTimer.Start();
        _cancellationToken = hostApplicationLifetime.ApplicationStopping;
    }

    /// <summary>
    /// Ensures connection and channel are established and healthy (Singleton pattern)
    /// </summary>
    public async Task EnsureConnectionAsync()
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
                    _logger.LogWarning("RabbitMQ connection shutdown: {ReplyText}", args.ReplyText);
                    await CloseConnectionAsync();
                };

                _connection.ConnectionRecoveryErrorAsync += async (sender, args) =>
                {
                    _logger.LogError("RabbitMQ connection recovery error: {Message}", args.Exception.Message);
                    await CloseConnectionAsync();
                };

                _connection.RecoverySucceededAsync += async (sender, args) =>
                {
                    _logger.LogInformation("RabbitMQ connection recovery succeeded");
                };
            }

            _logger.LogInformation("Connected to RabbitMQ");
        }
        finally
        {
            _reconnectSemaphore.Release();
        }
    }

    public async Task PublishAsync(
        string exchange,
        string routingKey,
        string fireInstanceId,
        string command,
        string body,
        int copies = 1)
    {
        await EnsureConnectionAsync();
        ArgumentNullException.ThrowIfNull(_connection);

        var bodyBytes = Encoding.UTF8.GetBytes(body);

        // 1. Enable confirms when creating the channel
        var channelOptions = new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true
        );

        using var channel = await _connection.CreateChannelAsync(channelOptions, cancellationToken: _cancellationToken);
        var properties = new BasicProperties
        {
            Persistent = true,
            Headers = new Dictionary<string, object?>
            {
                { "FireInstanceId", fireInstanceId },
                { "Command", command }
            }
        };

        for (int i = 0; i < copies; i++)
        {
            await channel.BasicPublishAsync(
                  exchange: exchange,
                  routingKey: routingKey,
                  mandatory: true,
                  basicProperties: properties,
                  body: bodyBytes);
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

    /// <summary>
    /// Closes the channel and connection gracefully
    /// </summary>
    private async Task CloseConnectionAsync()
    {
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
        try
        {
            _healthCheckTimer.Stop();
            await EnsureConnectionAsync();
            healthCheckFailCounter = 0;
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref healthCheckFailCounter);
            _logger.LogError(ex, "RabbitMQ health check failed on attempt {Attempt}", healthCheckFailCounter);
        }
        finally
        {
            _healthCheckTimer.Start();
        }
    }
}