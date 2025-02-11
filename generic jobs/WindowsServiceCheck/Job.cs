using Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Job;
using System.ComponentModel;
using System.ServiceProcess;

namespace WindowsServiceCheck;

internal partial class Job : BaseCheckJob
{
#pragma warning disable S3251 // Implementations should be provided for "partial" methods

    static partial void CustomConfigure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context);

    static partial void VetoService(Service service);

    static partial void VetoHost(Host host);

    static partial void Finalayze(IEnumerable<Service> services);

#pragma warning restore S3251 // Implementations should be provided for "partial" methods

    public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
        => CustomConfigure(configurationBuilder, context);

    public async override Task ExecuteJob(IJobExecutionContext context)
    {
        Initialize(ServiceProvider);

        var defaults = GetDefaults(Configuration);
        var hosts = GetHosts(Configuration, h => VetoHost(h));
        var services = GetServices(Configuration, defaults);

        ValidateRequired(hosts, "hosts");
        ValidateRequired(services, "services");

        services = GetServicesWithHost(services, hosts);

        EffectedRows = 0;

        await SafeInvokeCheck(services, InvokeServicesInner, context.TriggerDetails);

        Finalayze(services);
        Finalayze();
    }

    public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
    {
        services.RegisterSpanCheck();
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
            VetoService(service);
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
        await Task.Run(() => InvokeServiceInner1(service));
    }

#pragma warning disable CA1416 // Validate platform compatibility

    private void InvokeServiceInner1(Service service)
    {
        try
        {
            InvokeServiceInner2(service);
        }
        catch (InvalidOperationException ex)
        {
            if (ex.InnerException is Win32Exception win32Exception)
            {
                throw new CheckException($"service {service.Name} on host {service.Host} fail. message: {win32Exception.Message}");
            }

            throw;
        }
    }

    private void InvokeServiceInner2(Service service)
    {
        if (string.IsNullOrWhiteSpace(service.Host))
        {
            throw new CheckException($"service '{service.Name}' has no host. (missing host group name '{service.HostGroupName}'");
        }

        using var controller = new ServiceController(service.Name, service.Host);
        var status = controller.Status;
        var startType = controller.StartType;
        var disabled = status == ServiceControllerStatus.Stopped && startType == ServiceStartMode.Disabled;

        service.Result.Disabled = disabled;

        if (disabled && service.IgnoreDisabled)
        {
            Logger.LogInformation("skipping disabled service '{Name}' on host '{Host}'", service.Name, service.Host);
            return;
        }

        if (disabled)
        {
            throw new CheckException($"service '{service.Name}' on host '{service.Host}' is in {status} start type");
        }

        service.Result.AutoStartMode = startType == ServiceStartMode.Automatic;

        if (startType == ServiceStartMode.Manual && service.AutoStartMode)
        {
            throw new CheckException($"service '{service.Name}' on host '{service.Host}' start type is {nameof(ServiceStartMode.Manual)}");
        }

        if (status == ServiceControllerStatus.Running)
        {
            Logger.LogInformation("service '{Name}' on host '{Host}' is in running status", service.Name, service.Host);
            IncreaseEffectedRows();
            return;
        }

        if (status == ServiceControllerStatus.StartPending || status == ServiceControllerStatus.ContinuePending)
        {
            Logger.LogWarning("service '{Name}' on host '{Host}' is in {Status} status. waiting for running status...", service.Name, service.Host, status);
            controller.WaitForStatus(ServiceControllerStatus.Running, service.StartServiceTimeout);
            controller.Refresh();
            status = controller.Status;
            if (status == ServiceControllerStatus.Running)
            {
                Logger.LogInformation("service '{Name}' on host '{Host}' is in running status", service.Name, service.Host);
                IncreaseEffectedRows();
                return;
            }
        }

        if ((status == ServiceControllerStatus.StopPending) && service.StartService)
        {
            Logger.LogWarning("service '{Name}' on host '{Host}' is in {Status} status. waiting for stopped status...", service.Name, service.Host, status);
            controller.WaitForStatus(ServiceControllerStatus.StopPending, TimeSpan.FromSeconds(30));
            controller.Refresh();
            status = controller.Status;
        }

        if (status == ServiceControllerStatus.Stopped && service.StartService)
        {
            Logger.LogWarning("service '{Name}' on host '{Host}' is in stopped status. starting service", service.Name, service.Host);
            controller.Start();
            controller.WaitForStatus(ServiceControllerStatus.Running, service.StartServiceTimeout);
            controller.Refresh();
            status = controller.Status;
            if (status == ServiceControllerStatus.Running)
            {
                service.Result.Started = true;
                Logger.LogInformation("service '{Name}' on host '{Host}' is in running status", service.Name, service.Host);
                IncreaseEffectedRows();
                return;
            }
        }

        if (status == ServiceControllerStatus.Paused && service.StartService)
        {
            Logger.LogWarning("service '{Name}' on host '{Host}' is in paused status. continue service", service.Name, service.Host);
            controller.Continue();
            controller.WaitForStatus(ServiceControllerStatus.Running, service.StartServiceTimeout);
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
        ValidateGreaterThen(service.StartServiceTimeout, TimeSpan.FromSeconds(5), "start service timeout", section);
        ValidateLessThen(service.StartServiceTimeout, TimeSpan.FromMinutes(5), "start service timeout", section);
    }
}