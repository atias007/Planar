using Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Job;

namespace RabbitMQCheck;

public class Job : BaseCheckJob
{
    public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
    {
    }

    public async override Task ExecuteJob(IJobExecutionContext context)
    {
        Initialize(ServiceProvider);

        var tasks = new List<Task>();
        var defaults = GetDefaults(Configuration);
        var server = GetServer(Configuration);
        var healthCheck = GetHealthCheck(Configuration, defaults);
        var node = GetNode(Configuration, defaults);
        var queues = GetQueue(Configuration, defaults);

        // health check
        var healthCheckTask = InvokeHealthCheck(healthCheck, server);
        tasks.Add(healthCheckTask);

        // nodes
        var nodeCheckTask = SafeInvokeNodeCheck(node, server);
        tasks.Add(nodeCheckTask);

        // queues
        var queueTask = SafeInvokeQueueCheck(queues, server, defaults);
        tasks.Add(queueTask);

        await Task.WhenAll(tasks);

        CheckAggragateException();
        HandleCheckExceptions();
    }

    public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
    {
        services.AddSingleton<Defaults>();
        services.RegisterBaseCheck();
    }

    private static HealthCheck GetHealthCheck(IConfiguration configuration, Defaults defaults)
    {
        var section = configuration.GetSection("health check");
        var healthCheck = new HealthCheck(section);
        FillBase(healthCheck, defaults);
        return healthCheck;
    }

    private static Node GetNode(IConfiguration configuration, Defaults defaults)
    {
        var section = configuration.GetSection("nodes");
        var node = new Node(section);
        FillBase(node, defaults);
        return node;
    }

    private static IEnumerable<Queue> GetQueue(IConfiguration configuration, Defaults defaults)
    {
        var sections = configuration.GetSection("queues").GetChildren();

        foreach (var section in sections)
        {
            var queue = new Queue(section);
            queue.SetSize();
            FillBase(queue, defaults);

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
        var section = configuration.GetSection("server");
        var server = new Server(section);

        ValidateRequired(server.Hosts, "hosts", "server");
        ValidateRequired(server.Username, "username", "server");
        ValidateRequired(server.Password, "password", "server");

        foreach (var item in server.Hosts)
        {
            ValidateUri(item, "hosts", "server");
        }

        ValidateRequired(server.Hosts, "hosts", "server");

        return server;
    }

    private Defaults GetDefaults(IConfiguration configuration)
    {
        var def = ServiceProvider.GetRequiredService<Defaults>();
        var section = configuration.GetSection("defaults");
        if (section == null)
        {
            Logger.LogWarning("no defaults section found on settings file. set job factory defaults");
            return def;
        }

        def.RetryCount = section.GetValue<int?>("retry count") ?? def.RetryCount;
        def.RetryInterval = section.GetValue<TimeSpan?>("retry interval") ?? def.RetryInterval;
        def.MaximumFailsInRow = section.GetValue<int?>("maximum fails in row") ?? def.MaximumFailsInRow;

        ValidateBase(def, "defaults");

        return def;
    }

    private async Task InvokeHealthCheckInner(HealthCheck healthCheck, Server server, string host)
    {
        if (!healthCheck.Active)
        {
            Logger.LogInformation("Skipping inactive health check");
            return;
        }

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
        if (!node.Active)
        {
            Logger.LogInformation("skipping inactive nodes");
            return;
        }

        if (!node.IsValid) { return; }

        var proxy = RabbitMqProxy.GetProxy(host, server, Logger);
        var details = await proxy.GetNodeDetails();

        foreach (var item in details)
        {
            if (node.DiskFreeAlarm.GetValueOrDefault())
            {
                if (item.DiskFreeAlarm)
                {
                    throw new CheckException($"node check (disk free alarm) on host {host} failed. free disk is {item.DiskFree:N0} and limit is {item.DiskFreeLimit:N0}");
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
                    throw new CheckException($"node check (memory alarm) on host {host} failed. used memory is {item.MemoryUsed:N0} and limit is {item.MemoryLimit:N0}");
                }
                else
                {
                    Logger.LogInformation("node check (memory alarm) on host {Host} succeeded", host);
                }
            }
        }
    }

    private async Task InvokeHealthCheck(HealthCheck healthCheck, Server server)
    {
        foreach (var host in server.Hosts)
        {
            await SafeInvokeCheck(healthCheck, hc => InvokeHealthCheckInner(hc, server, host));
        }
    }

    private async Task InvokeQueueCheckInner(Queue queue, Server server, IEnumerable<QueueDetails> details)
    {
        if (!queue.Active)
        {
            Logger.LogInformation("skipping inactive queue '{Name}'", queue.Name);
            return;
        }

        if (!queue.IsValid) { return; }
        var host = server.DefaultHost;

        var detail = details.FirstOrDefault(x => string.Equals(x.Name, queue.Name, StringComparison.OrdinalIgnoreCase))
            ?? throw new CheckException($"queue check (exists) on host {host} failed. queue '{queue.Name}' does not exists");

        CheckState(host, queue, detail);
        CheckConsumers(host, queue, detail);
        CheckMessages(host, queue, detail);
        CheckMemory(host, queue, detail);

        await Task.CompletedTask;
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
                throw new CheckException($"queue check (memory), name '{detail.Name}' on host {host} failed. value is '{detail.Memory:N0}'");
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
                throw new CheckException($"queue check (messages), name '{detail.Name}' on host {host} failed. value is '{detail.Messages:N0}'");
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
                throw new CheckException($"queue check (consumers), name '{detail.Name}' on host {host} failed. value is '{detail.Consumers:N0}'");
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
                throw new CheckException($"queue check (state), name '{detail.Name}' on host {host} failed. queue '{queue.Name}' is in state '{detail.State}'");
            }
        }
    }

    private async Task SafeInvokeQueueCheck(IEnumerable<Queue> queues, Server server, Defaults defaults)
    {
        if (queues == null) { return; }
        if (!queues.Any()) { return; }
        if (queues.All(q => !q.IsValid)) { return; }

        var details = await SafeInvokeFunction(() =>
        {
            var host = server.DefaultHost;
            var proxy = RabbitMqProxy.GetProxy(host, server, Logger);
            return proxy.GetQueueDetails();
        }, defaults);

        if (details == null) { return; }

        var tasks = SafeInvokeCheck(queues, q => InvokeQueueCheckInner(q, server, details));
        await Task.WhenAll(tasks);
    }

    private async Task SafeInvokeNodeCheck(Node node, Server server)
    {
        foreach (var host in server.Hosts)
        {
            await SafeInvokeCheck(node, n => InvokeNodeCheckInner(n, server, host));
        }
    }
}