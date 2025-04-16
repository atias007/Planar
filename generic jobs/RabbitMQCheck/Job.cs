using Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet.Internal;
using Newtonsoft.Json;
using Planar.Job;
using System.Text;
using System.Xml.Linq;

namespace RabbitMQCheck;

internal partial class Job : BaseCheckJob
{
    private const string BundleEntity = "bundle";
    private const string QueueEntity = "queue";

#pragma warning disable S3251 // Implementations should be provided for "partial" methods

    partial void CustomConfigure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context);

    partial void CustomConfigure(RabbitMqServer rabbitMqServer, IConfiguration configuration);

    partial void VetoQueue(Queue queue);

    partial void VetoQueuesBundle(QueuesBundle bundle);

    partial void Finalayze(FinalayzeDetails<IEnumerable<Queue>> details);

    partial void Finalayze(FinalayzeDetails<IEnumerable<QueuesBundle>> details);

    partial void Finalayze(FinalayzeDetails<IEnumerable<Node>> details);

    partial void Finalayze(FinalayzeDetails<IEnumerable<HealthCheck>> details);

    public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
    {
        CustomConfigure(configurationBuilder, context);

        var rabbitmqServer = new RabbitMqServer();
        CustomConfigure(rabbitmqServer, configurationBuilder.Build());

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
        var keys = GetKeys(context);
        var queues = GetQueue(Configuration, defaults, keys);
        var bundles = GetQueuesBundle(Configuration, defaults, keys);

        EffectedRows = 0;

        // health check
        var checks = server.Hosts.Select(h => new HealthCheck(healthCheck, h));
        var healthCheckTask = SafeInvokeCheck(checks, hc => InvokeHealthCheckInner(hc, server), context.TriggerDetails);
        tasks.Add(healthCheckTask);

        // nodes
        var nodes = server.Hosts.Select(h => new Node(node, h));
        var nodeCheckTask = SafeInvokeCheck(nodes, n => InvokeNodeCheckInner(n, server), context.TriggerDetails);
        tasks.Add(nodeCheckTask);

        // queues
        var queueTask = SafeInvokeQueueCheck(queues, server, defaults, context.TriggerDetails);
        tasks.Add(queueTask);

        // queues
        var bundleTask = SafeInvokeQueueCheck(bundles, server, defaults, context.TriggerDetails);
        tasks.Add(bundleTask);

        await Task.WhenAll(tasks);

        var hcDetails = GetFinalayzeDetails(checks.AsEnumerable());
        Finalayze(hcDetails);
        var hcNodes = GetFinalayzeDetails(nodes.AsEnumerable());
        Finalayze(hcNodes);
        var queueDetails = GetFinalayzeDetails(queues.AsEnumerable());
        Finalayze(queueDetails);
        var bundleDetails = GetFinalayzeDetails(bundles.AsEnumerable());
        Finalayze(bundleDetails);
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

    private List<Queue> GetQueue(IConfiguration configuration, Defaults defaults)
    {
        var sections = configuration.GetSection("queues").GetChildren();
        var result = new List<Queue>();
        foreach (var section in sections)
        {
            var queue = new Queue(section, defaults);

            VetoQueue(queue);
            if (CheckVeto(queue, "queue")) { continue; }
            ValidateBase(queue, "queues");
            ValidateQueue(queue);

            result.Add(queue);
        }

        return result;
    }

    private List<Queue> GetQueue(IConfiguration configuration, Defaults defaults, IEnumerable<string>? keys)
    {
        if (keys == null || !keys.Any()) { return GetQueue(configuration, defaults); }

        var sections = configuration.GetSection("queues").GetChildren();
        var result = new List<Queue>();
        foreach (var section in sections)
        {
            var queue = new Queue(section, defaults);
            if (keys.Any(k => string.Equals(k, queue.Key, StringComparison.OrdinalIgnoreCase)))
            {
                queue.BindToTriggers = null;
                ValidateBase(queue, "queues");
                ValidateQueue(queue);
            }

            result.Add(queue);
        }

        return result;
    }

    private List<QueuesBundle> GetQueuesBundle(IConfiguration configuration, Defaults defaults)
    {
        var sections = configuration.GetSection("queues bundles").GetChildren();
        var result = new List<QueuesBundle>();

        foreach (var section in sections)
        {
            var bundle = new QueuesBundle(section, defaults);

            VetoQueuesBundle(bundle);
            if (CheckVeto(bundle, "queues bundle")) { continue; }
            ValidateBase(bundle, "queues bundles");
            ValidateQueue(bundle);

            // validate queues
            ValidateRequired(bundle.Queues, "queues", "queues bundles");
            ValidateDuplicates(bundle.Queues, "queues bundles --> queues");
            ValidateNullOrWhiteSpace(bundle.Queues, "queues bundles --> queues");

            result.Add(bundle);
        }

        return result;
    }

    private List<QueuesBundle> GetQueuesBundle(IConfiguration configuration, Defaults defaults, IEnumerable<string>? keys)
    {
        if (keys == null || !keys.Any()) { return GetQueuesBundle(configuration, defaults); }

        var sections = configuration.GetSection("queues bundles").GetChildren();
        var result = new List<QueuesBundle>();

        foreach (var section in sections)
        {
            var bundle = new QueuesBundle(section, defaults);
            if (keys.Any(k => string.Equals(k, bundle.Key, StringComparison.OrdinalIgnoreCase)))
            {
                bundle.BindToTriggers = null;
                ValidateBase(bundle, "queues bundles");
                ValidateQueue(bundle);

                // validate queues
                ValidateRequired(bundle.Queues, "queues", "queues bundles");
                ValidateDuplicates(bundle.Queues, "queues bundles --> queues");
                ValidateNullOrWhiteSpace(bundle.Queues, "queues bundles --> queues");

                result.Add(bundle);
            }
        }

        return result;
    }

    private static void ValidateQueue(Queue queue)
    {
        ValidateRequired(queue.Name, "name", "queues");
        ValidateGreaterThen(queue.Messages, 0, "messages", "queues");
        ValidateGreaterThen(queue.MemoryNumber, 0, "memory", "queues");
        ValidateGreaterThen(queue.Consumers, 0, "consumers", "queues");
        ValidateRequired(queue.CheckState, "check state", "queues");
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

    private async Task InvokeHealthCheckInner(HealthCheck healthCheck, Server server)
    {
        if (!healthCheck.IsValid) { return; }
        if (string.IsNullOrEmpty(healthCheck.Host)) { return; }

        var proxy = RabbitMqProxy.GetProxy(healthCheck.Host, server, Logger);

        try
        {
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
        }
        catch (Exception ex)
        {
            healthCheck.ResultMessage = ex.Message;
            throw;
        }

        IncreaseEffectedRows();
    }

    private async Task InvokeNodeCheckInner(Node node, Server server)
    {
        if (!node.IsValid) { return; }
        var host = node.Host;
        if (string.IsNullOrWhiteSpace(host)) { return; }
        var proxy = RabbitMqProxy.GetProxy(host, server, Logger);
        var details = await proxy.GetNodeDetails();

        node.Result = details;

        foreach (var item in details)
        {
            if (node.DiskFreeAlarm.GetValueOrDefault())
            {
                if (item.DiskFreeAlarm)
                {
                    node.ResultMessage = $"node check (disk free alarm) on host {host} failed. free disk is {item.DiskFree:N0} and limit is {item.DiskFreeLimit:N0}";
                    throw new CheckException($"node check (disk free alarm) on host {host} failed. free disk is {item.DiskFree:N0} and limit is {item.DiskFreeLimit:N0}");
                }
                else
                {
                    node.ResultMessage = $"node check (disk free alarm) on host {host} succeeded";
                    Logger.LogInformation("node check (disk free alarm) on host {Host} succeeded", host);
                }
            }

            if (node.MemoryAlarm.GetValueOrDefault())
            {
                if (item.MemoryAlarm)
                {
                    node.ResultMessage = $"node check (memory alarm) on host {host} failed. used memory is {item.MemoryUsed:N0} and limit is {item.MemoryLimit:N0}";
                    throw new CheckException($"node check (memory alarm) on host {host} failed. used memory is {item.MemoryUsed:N0} and limit is {item.MemoryLimit:N0}");
                }
                else
                {
                    node.ResultMessage += $"\r\nnode check (memory alarm) on host {host} succeeded".Trim();
                    Logger.LogInformation("node check (memory alarm) on host {Host} succeeded", host);
                }
            }

            IncreaseEffectedRows();
        }
    }

    private static QueueResult GetQueueResult(string name, IEnumerable<QueueResult> details)
    {
        var detail = details.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase))
            ?? throw new CheckException($"queue '{name}' does not exists");

        return detail;
    }

    private static QueueResult GetQueueResult(IEnumerable<string> names, IEnumerable<QueueResult> details)
    {
        var detail = details
            .Where(d => names.Any(n => string.Equals(d.Name, n, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (detail.Count < names.Count())
        {
            var exists = detail.Select(d => d.Name);
            var missing = names.Where(n => !exists.Any(e => string.Equals(e, n, StringComparison.OrdinalIgnoreCase)));
            throw new CheckException($"queue(s) '{string.Join(',', missing)}' does not exists");
        }

        var result = new QueueResult
        {
            Name = string.Join(',', names),
            Consumers = detail.Sum(d => d.Consumers),
            Messages = detail.Sum(d => d.Messages),
            MessagesUnacknowledged = detail.Sum(d => d.MessagesUnacknowledged),
            Memory = detail.Sum(d => d.Memory),
            BundleStates = detail.ToDictionary(d => d.Name.ToLower(), d => d.State)
        };

        return result;
    }

    private async Task InvokeQueueCheckInner(Queue queue, IEnumerable<QueueResult> details)
    {
        if (!queue.IsValid) { return; }
        QueueResult detail;
        var bundle = queue as QueuesBundle;
        if (bundle != null)
        {
            detail = GetQueueResult(bundle.Queues, details);
        }
        else
        {
            detail = GetQueueResult(queue.Name, details);
        }

        queue.Result = detail;

        try
        {
            var isBundle = bundle != null;
            if (isBundle) { CheckState(bundle!, detail); } else { CheckState(queue, detail); }
            CheckConsumers(queue, detail, isBundle);
            CheckMessages(queue, detail, isBundle);
            CheckUnacked(queue, detail, isBundle);
            CheckMemory(queue, detail, isBundle);
        }
        catch (Exception ex)
        {
            queue.ResultMessage = ex.Message;
            throw;
        }

        IncreaseEffectedRows();
        await Task.CompletedTask;
    }

    private void CheckMemory(Queue queue, QueueResult detail, bool isBundle)
    {
        var entity = isBundle ? BundleEntity : QueueEntity;

        // Memory
        if (!queue.MemoryNumber.HasValue) { return; }
        if (queue.MemoryNumber.GetValueOrDefault() > detail.Memory)
        {
            Logger.LogInformation("{Entity} '{Name}' memory is ok. {Memory:N0}", entity, queue.Name, detail.Memory);
        }
        else
        {
            throw new CheckException($"{entity} '{queue.Name}' memory check failed. {detail.Memory:N0} is greater then {queue.MemoryNumber:N0} bytes");
        }
    }

    private void CheckMessages(Queue queue, QueueResult detail, bool isBundle)
    {
        var entity = isBundle ? BundleEntity : QueueEntity;

        // Messages
        if (!queue.Messages.HasValue) { return; }
        if (queue.Messages.GetValueOrDefault() >= detail.Messages)
        {
            Logger.LogInformation("{Entity} '{Name}' messages is ok. {Messages:N0} messages", entity, queue.Name, detail.Messages);
        }
        else
        {
            throw new CheckException($"{entity} '{queue.Name}' messages check failed. {detail.Messages:N0} messages are greater then {queue.Messages.GetValueOrDefault():N0}");
        }
    }

    private void CheckUnacked(Queue queue, QueueResult detail, bool isBundle)
    {
        var entity = isBundle ? BundleEntity : QueueEntity;

        if (!queue.Unacked.HasValue) { return; }
        if (detail.Messages == 0) // there is messages in queue
        {
            Logger.LogInformation("{Entity} '{Name}' unacked is ok. no messages in queue", entity, queue.Name);
            return;
        }

        if (
            detail.Messages > detail.MessagesUnacknowledged && // there is enough messages in queue so unacked cen be in maximum level
            queue.Unacked.GetValueOrDefault() < detail.MessagesUnacknowledged)
        {
            Logger.LogInformation("{Entity} '{Name}' unacked is ok. {Unacked:N0} unacked", entity, queue.Name, detail.MessagesUnacknowledged);
        }
        else
        {
            throw new CheckException($"{entity} '{queue.Name}' unacked check failed. {detail.MessagesUnacknowledged:N0} unacked are greater then {queue.Messages.GetValueOrDefault():N0}");
        }
    }

    private void CheckConsumers(Queue queue, QueueResult detail, bool isBundle)
    {
        var entity = isBundle ? BundleEntity : QueueEntity;

        // Consumers
        if (!queue.Consumers.HasValue) { return; }
        if (queue.Consumers.GetValueOrDefault() <= detail.Consumers)
        {
            Logger.LogInformation("{Entity} '{Name}' consumers is ok. {Consumers:N0} consumers", entity, queue.Name, detail.Consumers);
        }
        else
        {
            throw new CheckException($"{entity} '{queue.Name}' consumers check failed. {detail.Consumers:N0} consumers are less then {queue.Consumers.GetValueOrDefault():N0}");
        }
    }

    private void CheckState(Queue queue, QueueResult detail)
    {
        // Check State
        if (!queue.CheckState.GetValueOrDefault()) { return; }
        var ok = string.Equals(detail.State, "running", StringComparison.OrdinalIgnoreCase) || string.Equals(detail.State, "idle", StringComparison.OrdinalIgnoreCase);
        if (ok)
        {
            Logger.LogInformation("{Entity} '{Name}' state is ok", QueueEntity, queue.Name);
        }
        else
        {
            throw new CheckException($"{QueueEntity} '{queue.Name}' state check failed. state is '{detail.State}'");
        }
    }

    private void CheckState(QueuesBundle bundle, QueueResult detail)
    {
        // Check State
        if (!bundle.CheckState.GetValueOrDefault()) { return; }
        foreach (var queue in bundle.Queues)
        {
            var state = detail.BundleStates[queue.ToLower()];
            var ok = string.Equals(state, "running", StringComparison.OrdinalIgnoreCase) || string.Equals(state, "idle", StringComparison.OrdinalIgnoreCase);
            if (!ok)
            {
                throw new CheckException($"{BundleEntity} '{queue}' state check failed. state is '{detail.State}'");
            }
        }

        Logger.LogInformation("{Entity} '{Name}' state is ok", BundleEntity, bundle.Name);
    }

    private async Task SafeInvokeQueueCheck(IEnumerable<Queue> queues, Server server, Defaults defaults, ITriggerDetail trigger)
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

        await SafeInvokeCheck(queues, q => InvokeQueueCheckInner(q, details), trigger);
    }
}