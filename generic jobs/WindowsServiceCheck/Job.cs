using Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Job;
using System;
using System.ServiceProcess;

namespace WindowsServiceCheck;

internal sealed partial class Job : BaseCheckJob
{
    public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
    {
    }

    public async override Task ExecuteJob(IJobExecutionContext context)
    {
        Initialize(ServiceProvider);

        var defaults = GetDefaults(Configuration);
        var hosts = GetHosts(Configuration);
        var services = GetServices(Configuration, defaults, hosts);

        InitializeVariables(services);
        using var client = new HttpClient();
        var tasks = SafeInvokeCheck(services, InvokeServicesInner);
        await Task.WhenAll(tasks);

        CheckAggragateException();
        HandleCheckExceptions();
    }

    private void InitializeVariables(IEnumerable<Service> services)
    {
        _total = services.Where(f => f.Active).Select(f => f.Hosts.Count()).Sum();
        EffectedRows = 0;
    }

    private static void ValidateServices(IEnumerable<Service> services)
    {
        ValidateRequired(services, "services");
        ValidateDuplicateNames(services, "services");
    }

    public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
    {
        services.RegisterBaseCheck();
    }

    private static void FillDefaults(Service service, Defaults defaults)
    {
        SetDefaultName(service, () => service.Name);
        FillBase(service, defaults);
    }

    private static List<Service> GetServices(IConfiguration configuration, Defaults defaults, IEnumerable<string> hosts)
    {
        var services = configuration.GetRequiredSection("services");
        var result = new List<Service>();
        foreach (var item in services.GetChildren())
        {
            var service = new Service(item);
            FillDefaults(service, defaults);
            if (service.Hosts == null || !service.Hosts.Any())
            {
                service.SetHosts(hosts);
            }

            service.ClearInvalidHosts();
            ValidateService(service);
            result.Add(service);
        }

        ValidateServices(result);
        return result;
    }

    private static string[] GetHosts(IConfiguration configuration)
    {
        var hosts = configuration.GetSection("hosts");
        if (hosts == null) { return []; }
        var result = hosts.Get<string[]>() ?? [];
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
        Parallel.ForEach(service.Hosts, host => InvokeServiceInner(service, host));
        await Task.CompletedTask;
    }

#pragma warning disable CA1416 // Validate platform compatibility

    private void InvokeServiceInner(Service service, string host)
    {
        if (!service.Active)
        {
            Logger.LogInformation("skipping inactive service '{Name}'", service.Name);
            return;
        }

        UpdateProgress();
        using var controller = new ServiceController(service.Name, host);
        var status = controller.Status;
        var startType = controller.StartType;
        var disabled = status == ServiceControllerStatus.Stopped && startType == ServiceStartMode.Disabled;
        if (disabled && service.IgnoreDisabled)
        {
            Logger.LogInformation("skipping disabled service '{Name}' on host '{Host}'", service.Name, host);
            return;
        }

        if (disabled)
        {
            throw new CheckException($"service '{service.Name}' on host '{host}' is in {status} start type");
        }

        if (startType == ServiceStartMode.Manual && service.AutomaticStart)
        {
            throw new CheckException($"service '{service.Name}' start type is {nameof(ServiceStartMode.Manual)}");
        }

        if (status == ServiceControllerStatus.Running)
        {
            Logger.LogInformation("service '{Name}' on host '{Host}' is in running status", service.Name, host);
            IncreaseEffectedRows();
            return;
        }

        if (status == ServiceControllerStatus.StartPending || status == ServiceControllerStatus.ContinuePending)
        {
            Logger.LogWarning("service '{Name}' on host '{Host}' is in {Status} status. waiting for running status...", service.Name, host, status);
            controller.WaitForStatus(ServiceControllerStatus.Running, service.StartServiceTimeout);
            controller.Refresh();
            status = controller.Status;
            if (status == ServiceControllerStatus.Running)
            {
                Logger.LogInformation("service '{Name}' on host '{Host}' is in running status", service.Name, host);
                IncreaseEffectedRows();
                return;
            }
        }

        if ((status == ServiceControllerStatus.StopPending) && service.StartService)
        {
            Logger.LogWarning("service '{Name}' on host '{Host}' is in {Status} status. waiting for stopped status...", service.Name, host, status);
            controller.WaitForStatus(ServiceControllerStatus.StopPending, TimeSpan.FromSeconds(30));
            controller.Refresh();
            status = controller.Status;
        }

        if (status == ServiceControllerStatus.Stopped && service.StartService)
        {
            Logger.LogWarning("service '{Name}' on host '{Host}' is in stopped status. starting service", service.Name, host);
            controller.Start();
            controller.WaitForStatus(ServiceControllerStatus.Running, service.StartServiceTimeout);
            controller.Refresh();
            status = controller.Status;
            if (status == ServiceControllerStatus.Running)
            {
                Logger.LogInformation("service '{Name}' on host '{Host}' is in running status", service.Name, host);
                IncreaseEffectedRows();
                return;
            }
        }

        if (status == ServiceControllerStatus.Paused && service.StartService)
        {
            Logger.LogWarning("service '{Name}' on host '{Host}' is in paused status. continue service", service.Name, host);
            controller.Continue();
            controller.WaitForStatus(ServiceControllerStatus.Running, service.StartServiceTimeout);
            controller.Refresh();
            status = controller.Status;
            if (status == ServiceControllerStatus.Running)
            {
                Logger.LogInformation("service '{Name}' on host '{Host}' is in running status", service.Name, host);
                IncreaseEffectedRows();
                return;
            }
        }

        if (status != ServiceControllerStatus.Running)
        {
            throw new CheckException($"service '{service.Name}' on host '{host}' is in {status} status");
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
        ValidateRequired(service.Hosts, "hosts", section);
    }
}