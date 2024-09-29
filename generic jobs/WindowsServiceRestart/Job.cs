using Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Job;
using System.Net;
using System.ServiceProcess;

namespace WindowsServiceRestart;

internal partial class Job : BaseCheckJob
{
#pragma warning disable S3251 // Implementations should be provided for "partial" methods

    static partial void CustomConfigure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context);

    static partial void VetoService(ref Service service);

    static partial void VetoHost(ref Host host);

#pragma warning restore S3251 // Implementations should be provided for "partial" methods

    public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
        => CustomConfigure(configurationBuilder, context);

    public async override Task ExecuteJob(IJobExecutionContext context)
    {
        Initialize(ServiceProvider);

        var defaults = GetDefaults(Configuration);
        var hosts = GetHosts(Configuration, h => VetoHost(ref h));
        var services = GetServices(Configuration, defaults);

        ValidateRequired(hosts, "hosts");
        ValidateRequired(services, "services");

        services = GetServicesWithHost(services, hosts);

        EffectedRows = 0;

        using var client = new HttpClient();
        await SafeInvokeCheck(services, InvokeServicesInner);

        Finalayze();
    }

    public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
    {
        services.RegisterBaseCheck();
        services.AddSingleton<CheckIntervalTracker>();
    }

    private static List<Service> GetServicesWithHost(List<Service> services, IReadOnlyDictionary<string, HostsConfig> hosts)
    {
        var result = new List<Service>();
        if (hosts.Count != 0)
        {
            foreach (var rel in services)
            {
                if (!hosts.TryGetValue(rel.HostGroupName ?? string.Empty, out var hostGroup)) { continue; }
                foreach (var host in hostGroup.Hosts)
                {
                    var clone = new Service(rel)
                    {
                        Host = host
                    };
                    result.Add(clone);
                }
            }
        }

        return result;
    }

    private List<Service> GetServices(IConfiguration configuration, Defaults defaults)
    {
        var result = new List<Service>();
        var services = configuration.GetRequiredSection("services");

        foreach (var item in services.GetChildren())
        {
            var service = new Service(item, defaults);
            VetoService(ref service);
            if (CheckVeto(service, "service")) { continue; }
            ValidateService(service);
            result.Add(service);
        }

        ValidateRequired(result, "services");
        ValidateDuplicateNames(result, "services");

        return result;
    }

    private Defaults GetDefaults(IConfiguration configuration)
    {
        var empty = Defaults.Empty;
        var section = GetDefaultSection(configuration, Logger);
        if (section == null) { return empty; }
        var result = new Defaults(section);
        ValidateBase(result, "defaults");
        return result;
    }

    private async Task InvokeServicesInner(Service service)
    {
        await Task.Run(() => InvokeServiceInner(service));
    }

#pragma warning disable CA1416 // Validate platform compatibility

    private void InvokeServiceInner(Service service)
    {
        if (string.IsNullOrWhiteSpace(service.Host))
        {
            throw new CheckException($"service '{service.Name}' has no host name (null or empty)");
        }

        if (!IsIntervalElapsed(service, service.Interval))
        {
            Logger.LogInformation("skipping service '{Name}' due to its interval", service.Name);
            return;
        }

        using var controller = new ServiceController(service.Name, service.Host);
        var status = controller.Status;
        var startType = controller.StartType;
        var disabled = status == ServiceControllerStatus.Stopped && startType == ServiceStartMode.Disabled;
        if (disabled && service.IgnoreDisabled)
        {
            Logger.LogInformation("skipping disabled service '{Name}' on host '{Host}'", service.Name, service.Host);
            return;
        }

        if (disabled)
        {
            throw new CheckException($"service '{service.Name}' on host '{service.Host}' is in {status} start type");
        }

        if (status == ServiceControllerStatus.StartPending || status == ServiceControllerStatus.ContinuePending)
        {
            Logger.LogInformation("service '{Name}' on host '{Host}' is in {Status} status. no need to restart", service.Name, service.Host, status);
            return;
        }

        if (status == ServiceControllerStatus.Running)
        {
            Logger.LogInformation("service '{Name}' on host '{Host}' is in running status. stop the service", service.Name, service.Host);
            controller.Stop();
            controller.WaitForStatus(ServiceControllerStatus.Stopped, service.Timeout);
            controller.Refresh();
            status = controller.Status;
        }

        if (status == ServiceControllerStatus.StopPending)
        {
            Logger.LogInformation("service '{Name}' on host '{Host}' is in {Status} status. waiting for stopped status...", service.Name, service.Host, status);
            controller.WaitForStatus(ServiceControllerStatus.StopPending, service.Timeout);
            controller.Refresh();
            status = controller.Status;
        }

        if (status == ServiceControllerStatus.Stopped)
        {
            Logger.LogInformation("service '{Name}' on host '{Host}' is in stopped status. starting service", service.Name, service.Host);
            controller.Start();
            controller.WaitForStatus(ServiceControllerStatus.Running, service.Timeout);
            controller.Refresh();
            status = controller.Status;
            if (status == ServiceControllerStatus.Running)
            {
                Logger.LogInformation("service '{Name}' on host '{Host}' is in running status", service.Name, service.Host);
                IncreaseEffectedRows();
                return;
            }
        }

        if (status == ServiceControllerStatus.Paused)
        {
            Logger.LogWarning("service '{Name}' on host '{Host}' is in paused status. continue service", service.Name, service.Host);
            controller.Continue();
            controller.WaitForStatus(ServiceControllerStatus.Running, service.Timeout);
            controller.Refresh();
            status = controller.Status;
            if (status == ServiceControllerStatus.Running)
            {
                Logger.LogInformation("service '{Name}' on host '{Host}' is in running status", service.Name, service.Host);
                IncreaseEffectedRows();
                return;
            }
        }

        if (status != ServiceControllerStatus.Running)
        {
            throw new CheckException($"service '{service.Name}' on host '{service.Host}' is in {status} status");
        }
    }

#pragma warning restore CA1416 // Validate platform compatibility

    private static void ValidateService(Service service)
    {
        var section = $"services ({service.Name})";
        ValidateBase(service, section);
        ValidateMaxLength(service.Name, 255, "name", section);
        ValidateRequired(service.Name, "name", section);
        ValidateGreaterThen(service.Timeout, TimeSpan.FromSeconds(5), "timeout", section);
        ValidateLessThen(service.Timeout, TimeSpan.FromMinutes(5), "timeout", section);
        ValidateRequired(service.Host, "host", section);
    }
}