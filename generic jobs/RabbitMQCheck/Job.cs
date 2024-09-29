using Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Planar.Job;
using System.Text;
using YamlDotNet.Core.Tokens;

namespace RabbitMQCheck;

internal partial class Job : BaseCheckJob
{
#pragma warning disable S3251 // Implementations should be provided for "partial" methods

    static partial void CustomConfigure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context);

    static partial void CustomConfigure(ref RabbitMqServer rabbitMqServer, IConfiguration configuration);

    static partial void VetoQueue(ref Queue queue);

    static partial void Finalayze(IEnumerable<Queue> queues);

    public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
    {
        CustomConfigure(configurationBuilder, context);

        var rabbitmqServer = new RabbitMqServer();
        CustomConfigure(ref rabbitmqServer, configurationBuilder.Build());

        if (!rabbitmqServer.IsEmpty)
        {
            var json = JsonConvert.SerializeObject(new { server = rabbitmqServer });

            // Create a JSON stream as a MemoryStream or directly from a file
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            // Add the JSON stream to the configuration builder
            configurationBuilder.AddJsonStream(stream);
        }
    }

#pragma warning restore S3251 // Implementations should be provided for "partial" methods

    public async override Task ExecuteJob(IJobExecutionContext context)
    {
        Initialize(ServiceProvider);

        var tasks = new List<Task>();
        var defaults = GetDefaults(Configuration);
        var server = GetServer(Configuration);
        var healthCheck = GetHealthCheck(Configuration, defaults);
        var node = GetNode(Configuration, defaults);
        var queues = GetQueue(Configuration, defaults);

        EffectedRows = 0;

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

        Finalayze(queues);
        Finalayze();
    }

    public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
    {
        services.RegisterSpanCheck();
    }

    private static HealthCheck GetHealthCheck(IConfiguration configuration, Defaults defaults)
    {
        var section = configuration.GetSection("health check");
        var healthCheck = new HealthCheck(section, defaults);
        return healthCheck;
    }

    private static Node GetNode(IConfiguration configuration, Defaults defaults)
    {
        var section = configuration.GetSection("nodes");
        var node = new Node(section, defaults);
        return node;
    }

    private IEnumerable<Queue> GetQueue(IConfiguration configuration, Defaults defaults)
    {
        var sections = configuration.GetSection("queues").GetChildren();

        foreach (var section in sections)
        {
            var queue = new Queue(section, defaults);

            VetoQueue(ref queue);
            if (CheckVeto(queue, "queue")) { continue; }

            ValidateRequired(queue.Name, "name", "queues");
            ValidateGreaterThen(queue.Messages, 0, "messages", "queues");
            ValidateGreaterThen(queue.MemoryNumber, 0, "memory", "queues");
            ValidateGreaterThen(queue.Consumers, 0, "consumers", "queues");
            ValidateRequired(queue.CheckState, "check state", "queues");

            if (queue.AllowedFailSpan.HasValue && queue.AllowedFailSpan.Value.TotalSeconds < 1)
            {
                throw new InvalidDataException($"'span' on queues section is less then 1 second");
            }

            yield return queue;
        }
    }

    private static Server GetServer(IConfiguration configuration)
    {
        var server = new Server(configuration);

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
        var section = GetDefaultSection(configuration, Logger);
        if (section == null) { return Defaults.Empty; }

        var result = new Defaults(section);
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

        if (healthCheck.VirtualHosts.GetValueOrDefault())
        {
            await proxy.VirtualHosts();
        }

        IncreaseEffectedRows();
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

            IncreaseEffectedRows();
        }
    }

    private async Task InvokeHealthCheck(HealthCheck healthCheck, Server server)
    {
        foreach (var host in server.Hosts)
        {
            await SafeInvokeCheck(healthCheck, hc => InvokeHealthCheckInner(hc, server, host));
        }
    }

    private async Task InvokeQueueCheckInner(Queue queue, IEnumerable<QueueDetails> details)
    {
        if (!queue.IsValid) { return; }
        var detail = details.FirstOrDefault(x => string.Equals(x.Name, queue.Name, StringComparison.OrdinalIgnoreCase))
            ?? throw new CheckException($"queue '{queue.Name}' does not exists");

        CheckState(queue, detail);
        CheckConsumers(queue, detail);
        CheckMessages(queue, detail);
        CheckMemory(queue, detail);
        IncreaseEffectedRows();
        await Task.CompletedTask;
    }

    private void CheckMemory(Queue queue, QueueDetails detail)
    {
        // Memory
        if (queue.MemoryNumber.HasValue)
        {
            if (queue.MemoryNumber.GetValueOrDefault() > detail.Memory)
            {
                Logger.LogInformation("queue '{Name}' memory is ok. {Memory:N0}", detail.Name, detail.Memory);
            }
            else
            {
                throw new CheckException($"queue '{detail.Name}' memory check failed. {detail.Memory:N0} is greater then {queue.MemoryNumber:N0} bytes");
            }
        }
    }

    private void CheckMessages(Queue queue, QueueDetails detail)
    {
        // Messages
        if (queue.Messages.HasValue)
        {
            if (queue.Messages.GetValueOrDefault() >= detail.Messages)
            {
                Logger.LogInformation("queue '{Name}' messages is ok. {Messages:N0} messages", detail.Name, detail.Messages);
            }
            else
            {
                throw new CheckException($"queue '{detail.Name}' messages check failed. {detail.Messages:N0} messages are greater then {queue.Messages.GetValueOrDefault():N0}");
            }
        }
    }

    private void CheckConsumers(Queue queue, QueueDetails detail)
    {
        // Consumers
        if (queue.Consumers.HasValue)
        {
            if (queue.Consumers.GetValueOrDefault() <= detail.Consumers)
            {
                Logger.LogInformation("queue '{Name}' consumers is ok. {Consumers:N0} consumers", detail.Name, detail.Consumers);
            }
            else
            {
                throw new CheckException($"queue '{detail.Name}' consumers check failed. {detail.Consumers:N0} consumers less are then {queue.Consumers.GetValueOrDefault():N0}");
            }
        }
    }

    private void CheckState(Queue queue, QueueDetails detail)
    {
        // Check State
        if (queue.CheckState.GetValueOrDefault())
        {
            var ok = string.Equals(detail.State, "running", StringComparison.OrdinalIgnoreCase) || string.Equals(detail.State, "idle", StringComparison.OrdinalIgnoreCase);
            if (ok)
            {
                Logger.LogInformation("queue '{Name}' state is ok", detail.Name);
            }
            else
            {
                throw new CheckException($"queue '{detail.Name}' state check failed. state is '{detail.State}'");
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

        await SafeInvokeCheck(queues, q => InvokeQueueCheckInner(q, details));
    }

    private async Task SafeInvokeNodeCheck(Node node, Server server)
    {
        foreach (var host in server.Hosts)
        {
            await SafeInvokeCheck(node, n => InvokeNodeCheckInner(n, server, host));
        }
    }
}