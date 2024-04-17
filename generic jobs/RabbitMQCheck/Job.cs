using Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Job;
using Polly;

namespace RabbitMQCheck;

public class Job : BaseCheckJob
{
    public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
    {
    }

    public async override Task ExecuteJob(IJobExecutionContext context)
    {
        var tasks = new List<Task>();
        var defaults = GetDefaults(Configuration);
        var server = GetServer(Configuration);
        var healthCheck = GetHealthCheck(Configuration);
        var node = GetNode(Configuration);
        var queues = GetQueue(Configuration);

        ValidateHosts(server.Hosts);
        CheckAggragateException();

        // health check
        FillBase(healthCheck, defaults);
        var healthCheckTask = SafeInvokeHealthCheck(healthCheck, server);
        tasks.Add(healthCheckTask);

        // nodes
        FillBase(node, defaults);
        var nodeCheckTask = SafeInvokeNodeCheck(node, server);
        tasks.Add(nodeCheckTask);

        // queues
        var container = new QueuesContainer(queues);
        FillBase(container, defaults);
        var queueTask = SafeInvokeQueueCheck(container, server, defaults);
        tasks.Add(queueTask);

        await Task.WhenAll(tasks);

        CheckAggragateException();
        HandleCheckExceptions("RabbitMQ", "element");
    }

    public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
    {
        services.AddSingleton<CheckFailCounter>();
        services.AddSingleton<CheckSpanTracker>();
    }

    private static HealthCheck GetHealthCheck(IConfiguration configuration)
    {
        var section = configuration.GetSection("health check");
        var healthCheck = new HealthCheck(section);
        return healthCheck;
    }

    private static Node GetNode(IConfiguration configuration)
    {
        var section = configuration.GetSection("nodes");
        var node = new Node(section);
        return node;
    }

    private static IEnumerable<Queue> GetQueue(IConfiguration configuration)
    {
        var sections = configuration.GetSection("queues").GetChildren();

        foreach (var section in sections)
        {
            var queue = new Queue(section);
            queue.SetSize();

            ValidateRequired(queue.Name, "name", "queues");
            ValidateGreaterThen(queue.Messages, 0, "messages", "queues");
            ValidateGreaterThen(queue.MemoryNumber, 0, "memory", "queues");
            ValidateGreaterThen(queue.Consumers, 0, "consumers", "queues");
            ValidateRequired(queue.CheckState, "check state", "queues");

            if (queue.Span.HasValue && queue.Span.Value.TotalSeconds < 1)
            {
                throw new InvalidDataException($"'span' on queues section is less then 1 second");
            }

            yield return queue;
        }
    }

    private static Server GetServer(IConfiguration configuration)
    {
        var section = configuration.GetSection("servers");
        var server = new Server(section);

        ValidateRequired(server.Hosts, "hosts", "servers");
        ValidateRequired(server.Username, "username", "servers");
        ValidateRequired(server.Password, "password", "servers");

        foreach (var item in server.Hosts)
        {
            ValidateUri(item, "hosts", "servers");
        }

        return server;
    }

    private Defaults GetDefaults(IConfiguration configuration)
    {
        var empty = Defaults.Empty;
        var defaults = configuration.GetSection("defaults");
        if (defaults == null)
        {
            Logger.LogWarning("no defaults section found on settings file. set job factory defaults");
            return empty;
        }

        var result = new Defaults
        {
            RetryCount = defaults.GetValue<int?>("retry count") ?? empty.RetryCount,
            RetryInterval = defaults.GetValue<TimeSpan?>("retry interval") ?? empty.RetryInterval,
            MaximumFailsInRow = defaults.GetValue<int?>("maximum fails in row") ?? empty.MaximumFailsInRow,
        };

        ValidateBase(result, "defaults");

        return result;
    }

    private async Task InvokeHealthCheckInner(HealthCheck healthCheck, Server server, string host)
    {
        if (!healthCheck.IsValid) { return; }

        var proxy = RabbitMqProxy.GetProxy(host, server, Logger);
        if (healthCheck.ClusterAlarm.GetValueOrDefault())
        {
            await proxy.ClusterAlarm();
        }

        if (healthCheck.LocalAlarm.GetValueOrDefault())
        {
            await proxy.LocalAlarm();
        }

        if (healthCheck.NodeMirrorSync.GetValueOrDefault())
        {
            await proxy.NodeMirrorSync();
        }

        if (healthCheck.NodeQuorumCritical.GetValueOrDefault())
        {
            await proxy.NodeQuorumCritical();
        }
    }

    private async Task InvokeNodeCheckInner(Node node, Server server, string host)
    {
        if (!node.IsValid) { return; }

        var proxy = RabbitMqProxy.GetProxy(host, server, Logger);
        var details = await proxy.GetNodeDetails();

        foreach (var item in details)
        {
            if (node.DiskFreeAlarm.GetValueOrDefault())
            {
                if (item.DiskFreeAlarm)
                {
                    throw new CheckException($"node check (disk free alarm) on host {host} failed. free disk is {item.DiskFree:N0} and limit is {item.DiskFreeLimit:N0}", "nodes --> disk free alarm");
                }
                else
                {
                    Logger.LogInformation("node check (disk free alarm) on host {Host} succeeded", host);
                }
            }

            if (node.MemoryAlarm.GetValueOrDefault())
            {
                if (item.MemoryAlarm)
                {
                    throw new CheckException($"node check (memory alarm) on host {host} failed. used memory is {item.MemoryUsed:N0} and limit is {item.MemoryLimit:N0}", "nodes --> disk free alarm");
                }
                else
                {
                    Logger.LogInformation("node check (memory alarm) on host {Host} succeeded", host);
                }
            }
        }
    }

    private void SafeHandleException<T>(string name, T checkElement, string host, Exception ex, CheckFailCounter counter)
        where T : BaseDefault, ICheckElemnt
    {
        try
        {
            var exception = ex is CheckException ? null : ex;

            if (exception == null)
            {
                Logger.LogError("{Name} check fail for host {Host}. reason: {Message}",
                  name, host, ex.Message);
            }
            else
            {
                Logger.LogError(exception, "{Name} check fail for host {Host}. reason: {Message}",
                    name, host, ex.Message);
            }

            var value = counter.IncrementFailCount(checkElement);

            if (value > checkElement.MaximumFailsInRow)
            {
                Logger.LogWarning("{Name} check fail for host {Host} but maximum fails in row reached. reason: {Message}",
                    name, host, ex.Message);
            }
            else
            {
                var hcException = new CheckException($"{name} check fail for host {host}. reason: {ex.Message}", "health check");

                AddCheckException(hcException);
            }
        }
        catch (Exception innerEx)
        {
            AddAggregateException(innerEx);
        }
    }

    private async Task SafeInvokeHealthCheck(HealthCheck healthCheck, Server server)
    {
        foreach (var item in server.Hosts)
        {
            await SafeInvokeHealthCheck(healthCheck, server, item);
        }
    }

    private async Task SafeInvokeHealthCheck(HealthCheck healthCheck, Server server, string host)
    {
        var counter = ServiceProvider.GetRequiredService<CheckFailCounter>();

        try
        {
            if (healthCheck.RetryCount == 0)
            {
                await InvokeHealthCheckInner(healthCheck, server, host);
                return;
            }

            await Policy.Handle<Exception>()
                    .WaitAndRetryAsync(
                        retryCount: healthCheck.RetryCount.GetValueOrDefault(),
                        sleepDurationProvider: _ => healthCheck.RetryInterval.GetValueOrDefault(),
                         onRetry: (ex, _) =>
                         {
                             var exception = ex is CheckException ? null : ex;
                             Logger.LogWarning(exception, "retry for health check at host {Host}. Reason: {Message}", host, ex.Message);
                         })
                    .ExecuteAsync(async () =>
                    {
                        await InvokeHealthCheckInner(healthCheck, server, host);
                    });

            counter.ResetFailCount(healthCheck);
        }
        catch (Exception ex)
        {
            SafeHandleException("health", healthCheck, host, ex, counter);
        }
    }

    private async Task InvokeQueueCheckInner(QueuesContainer container, Server server)
    {
        if (container.Queues == null) { return; }
        if (!container.Queues.Any()) { return; }
        if (container.Queues.All(q => !q.IsValid)) { return; }
        var host = server.DefaultHost;
        var proxy = RabbitMqProxy.GetProxy(host, server, Logger);
        var details = await proxy.GetQueueDetails();
        ////var counter = ServiceProvider.GetRequiredService<CheckFailCounter>();

        foreach (var queue in container.Queues)
        {
            if (!queue.IsValid) { continue; }
            var detail = details.FirstOrDefault(x => string.Equals(x.Name, queue.Name, StringComparison.OrdinalIgnoreCase))
                ?? throw new CheckException($"queue check on host {host} failed. queue '{queue.Name}' does not exists", queue.Name);

            CheckState(host, queue, detail);
            CheckConsumers(host, queue, detail);
            CheckMessages(host, queue, detail);
            CheckMemory(host, queue, detail);
        }
    }

    private void HandleQueueCheckExceptions(string message, Queue queue, string stage)
    {
        bool IsSpanValid(CheckSpanTracker spanner) =>
            queue.Span == null ||
            queue.Span == TimeSpan.Zero ||
            queue.Span > spanner.LastFailSpan(queue, stage);

        var spanner = ServiceProvider.GetRequiredService<CheckSpanTracker>();
        if (IsSpanValid(spanner))
        {
            var logMessage = $"{message} --> but error span is valid";
#pragma warning disable CA2254 // Template should be a static expression
            Logger.LogWarning(logMessage);
#pragma warning restore CA2254 // Template should be a static expression
            return;
        }

        var ex = new CheckException(message, queue.Name);
        AddCheckException(ex);
    }

    private void CheckMemory(string host, Queue queue, QueueDetails detail)
    {
        // Memory
        if (queue.MemoryNumber.HasValue)
        {
            if (queue.MemoryNumber.GetValueOrDefault() > detail.Memory)
            {
                Logger.LogInformation("queue check (memory), name '{Name}' on host {Host} succeeded. value is {Memory:N0}", detail.Name, host, detail.Memory);
            }
            else
            {
                HandleQueueCheckExceptions($"queue check (memory), name '{detail.Name}' on host {host} failed. value is '{detail.Memory:N0}'", queue, "memory");
            }
        }
    }

    private void CheckMessages(string host, Queue queue, QueueDetails detail)
    {
        // Messages
        if (queue.Messages.HasValue)
        {
            if (queue.Messages.GetValueOrDefault() >= detail.Messages)
            {
                Logger.LogInformation("queue check (messages), name '{Name}' on host {Host} succeeded. value is {Messages:N0}", detail.Name, host, detail.Messages);
            }
            else
            {
                HandleQueueCheckExceptions($"queue check (messages), name '{detail.Name}' on host {host} failed. value is '{detail.Messages:N0}'", queue, "messages");
            }
        }
    }

    private void CheckConsumers(string host, Queue queue, QueueDetails detail)
    {
        // Consumers
        if (queue.Consumers.HasValue)
        {
            if (queue.Consumers.GetValueOrDefault() <= detail.Consumers)
            {
                Logger.LogInformation("queue check (consumers), name '{Name}' on host {Host} succeeded. value is {Consumers:N0}", detail.Name, host, detail.Consumers);
            }
            else
            {
                HandleQueueCheckExceptions($"queue check (consumers), name '{detail.Name}' on host {host} failed. value is '{detail.Consumers:N0}'", queue, "consumers");
            }
        }
    }

    private void CheckState(string host, Queue queue, QueueDetails detail)
    {
        // Check State
        if (queue.CheckState.GetValueOrDefault())
        {
            var ok = string.Equals(detail.State, "running", StringComparison.OrdinalIgnoreCase) || string.Equals(detail.State, "idle", StringComparison.OrdinalIgnoreCase);
            if (ok)
            {
                Logger.LogInformation("queue check (state), name '{Name}' on host {Host} succeeded", detail.Name, host);
            }
            else
            {
                HandleQueueCheckExceptions($"queue check (state), name '{detail.Name}' on host {host} failed. queue '{queue.Name}' is in state '{detail.State}'", queue, "state");
            }
        }
    }

    private async Task SafeInvokeQueueCheck(QueuesContainer container, Server server, Defaults defaults)
    {
        var counter = ServiceProvider.GetRequiredService<CheckFailCounter>();

        try
        {
            if (defaults.RetryCount == 0)
            {
                await InvokeQueueCheckInner(container, server);
                return;
            }

            await Policy.Handle<Exception>()
                    .WaitAndRetryAsync(
                        retryCount: defaults.RetryCount.GetValueOrDefault(),
                        sleepDurationProvider: _ => defaults.RetryInterval.GetValueOrDefault(),
                         onRetry: (ex, _) =>
                         {
                             var exception = ex is CheckException ? null : ex;
                             Logger.LogWarning(exception, "retry for queue check at host {Host}. Reason: {Message}", server.DefaultHost, ex.Message);
                         })
                    .ExecuteAsync(async () =>
                    {
                        await InvokeQueueCheckInner(container, server);
                    });

            counter.ResetFailCount(container);
        }
        catch (Exception ex)
        {
            SafeHandleException("node", container, server.DefaultHost, ex, counter);
        }
    }

    private async Task SafeInvokeNodeCheck(Node node, Server server)
    {
        foreach (var item in server.Hosts)
        {
            await SafeInvokeNodeCheck(node, server, item);
        }
    }

    private async Task SafeInvokeNodeCheck(Node node, Server server, string host)
    {
        var counter = ServiceProvider.GetRequiredService<CheckFailCounter>();

        try
        {
            if (node.RetryCount == 0)
            {
                await InvokeNodeCheckInner(node, server, host);
                return;
            }

            await Policy.Handle<Exception>()
                    .WaitAndRetryAsync(
                        retryCount: node.RetryCount.GetValueOrDefault(),
                        sleepDurationProvider: _ => node.RetryInterval.GetValueOrDefault(),
                         onRetry: (ex, _) =>
                         {
                             var exception = ex is CheckException ? null : ex;
                             Logger.LogWarning(exception, "retry for node check at host {Host}. Reason: {Message}", host, ex.Message);
                         })
                    .ExecuteAsync(async () =>
                    {
                        await InvokeNodeCheckInner(node, server, host);
                    });

            counter.ResetFailCount(node);
        }
        catch (Exception ex)
        {
            SafeHandleException("node", node, host, ex, counter);
        }
    }

    private void ValidateHosts(IEnumerable<string> hosts)
    {
        try
        {
            CommonUtil.ValidateItems(hosts, "servers --> hosts");
        }
        catch (Exception ex)
        {
            AddAggregateException(ex);
        }
    }
}