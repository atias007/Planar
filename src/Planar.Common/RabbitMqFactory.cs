using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace Planar.Common;

public sealed class RabbitMqFactory
{
    private readonly CancellationToken _cancellationToken;
    private readonly ConnectionFactory _connectionFactory = new();
    private readonly bool _enabled;
    private readonly List<AmqpTcpEndpoint> _endpoints = [];
    private readonly Lock _healthCheckLock = new();
    private readonly ILogger<RabbitMqFactory> _logger;
    private readonly SemaphoreSlim _reconnectSemaphore = new(1, 1);
    private IConnection? _connection;
    private Timer? _healthCheckTimer;
    private int healthCheckFailCounter = 0;

    // CTOR //
    public RabbitMqFactory(IHostApplicationLifetime hostApplicationLifetime, ILogger<RabbitMqFactory> logger)
    {
        _logger = logger;
        _cancellationToken = hostApplicationLifetime.ApplicationStopping;
        _enabled = AppSettings.Hooks.RabbitMq.Endpoints != null && AppSettings.Hooks.RabbitMq.Endpoints.Any();
        InitializeConnectionFactory();
    }

    // PUBLISH //
    public async Task PublishAsync(
            string exchange,
            string routingKey,
            string? fireInstanceId,
            string command,
            string body,
            int copies = 1,
            int? timeoutSeconds = null)
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
                { "Command", command }
            }
        };

        if (!string.IsNullOrWhiteSpace(fireInstanceId))
        {
            properties.Headers["FireInstanceId"] = fireInstanceId;
        }

        if (timeoutSeconds.HasValue)
        {
            properties.Expiration = (timeoutSeconds.Value * 1000).ToString();
            properties.Headers["InvokeDeadline"] = DateTimeOffset.UtcNow.AddSeconds(timeoutSeconds.Value).ToUnixTimeMilliseconds();
        }

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
            StopHealthCheckTimer();
            if (_connection != null) { await _connection.CloseAsync(); }
            _connection?.Dispose();
            _connection = null;
        }
        catch
        {
            // DO NOTHING //
        }
    }

    /// <summary>
    /// Ensures connection and channel are established and healthy (Singleton pattern)
    /// </summary>
    private async Task EnsureConnectionAsync()
    {
        if (!_enabled)
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
                    StopHealthCheckTimer();
                    _connection = null;
                }

                var connectionName = $"{nameof(Planar)}:Server:{Environment.MachineName}";

                RestartHealthCheckTimer(60);

                // Create connection with 10-second timeout
                var connectionTask = _connectionFactory.CreateConnectionAsync(_endpoints, connectionName, _cancellationToken);
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10), _cancellationToken);
                var completedTask = await Task.WhenAny(connectionTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    throw new TimeoutException("Failed to establish RabbitMQ connection within 10 seconds");
                }

                _connection = await connectionTask;

                _logger.LogInformation("Connected to RabbitMQ");

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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to RabbitMQ");
            throw;
        }
        finally
        {
            _reconnectSemaphore.Release();
        }
    }

    private void InitializeConnectionFactory()
    {
        if (!_enabled) { return; }
        var settings = AppSettings.Hooks.RabbitMq;

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

        _endpoints.AddRange(settings.Endpoints.Select(e => new AmqpTcpEndpoint(e.Host, e.Port)));

        if (settings.Ssl != null)
        {
            _connectionFactory.Ssl = new SslOption
            {
                Enabled = settings.Ssl.Enable,
                ServerName = settings.Ssl.PolicyErrors,
                CertPassphrase = settings.Ssl.CertPassphrase
            };
        }
    }

    private void RestartHealthCheckTimer(int seconds)
    {
        lock (_healthCheckLock)
        {
            double interval = seconds * 1_000.0;

            if (_healthCheckTimer != null)
            {
                if (Math.Abs(_healthCheckTimer.Interval - interval) < 0.1)
                {
                    return;
                }

                _healthCheckTimer.Stop();
                _healthCheckTimer.Dispose();
            }

            _healthCheckTimer = new Timer(interval);
            _healthCheckTimer.Elapsed += async (sender, e) => await SafeHealthCheck();
        }
    }

    private async Task SafeHealthCheck()
    {
        try
        {
            StopHealthCheckTimer();
            await EnsureConnectionAsync();
            healthCheckFailCounter = 0;
            RestartHealthCheckTimer(60);
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref healthCheckFailCounter);
            if (healthCheckFailCounter >= 30 && healthCheckFailCounter < 60)
            {
                RestartHealthCheckTimer(300);
            }

            if (healthCheckFailCounter >= 60)
            {
                RestartHealthCheckTimer(1_200);
            }

            _logger.LogError(ex, "RabbitMQ health check failed on attempt {Attempt}", healthCheckFailCounter);
        }
    }

    private void StopHealthCheckTimer()
    {
        lock (_healthCheckLock)
        {
            if (_healthCheckTimer != null)
            {
                _healthCheckTimer.Stop();
                _healthCheckTimer.Dispose();
            }
        }
    }
}