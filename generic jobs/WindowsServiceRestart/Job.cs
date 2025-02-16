using Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Job;
using System.Globalization;
using System.Management;
using System.Net;
using System.Runtime.InteropServices;
using System.ServiceProcess;

namespace WindowsServiceRestart;

internal partial class Job : BaseCheckJob
{
#pragma warning disable S3251 // Implementations should be provided for "partial" methods

    static partial void CustomConfigure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context);

    static partial void VetoService(Service service);

    static partial void VetoHost(Host host);

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

        using var client = new HttpClient();
        await SafeInvokeOperation(services, InvokeServiceInner);

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

#pragma warning disable CA1416 // Validate platform compatibility

    private void InvokeServiceInner(Service service)
    {
        if (string.IsNullOrWhiteSpace(service.Host))
        {
            throw new CheckException($"service '{service.Name}' has no host name (null or empty)");
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
            controller.WaitForStatus(ServiceControllerStatus.Stopped, service.StopTimeout);
            controller.Refresh();
            status = controller.Status;
        }

        if (status == ServiceControllerStatus.StopPending)
        {
            if (service.KillProcess)
            {
                Logger.LogInformation("service '{Name}' on host '{Host}' is in stop pending status for long time (timeout). kill the process", service.Name, service.Host);
                KillServiceProcess(service.Name, service.Host);
                controller.Refresh();
                status = controller.Status;
            }
            else
            {
                throw new CheckException($"service '{service.Name}' on host '{service.Host}' is in stop pending status for long time (timeout)");
            }
        }

        if (status == ServiceControllerStatus.Stopped)
        {
            Logger.LogInformation("service '{Name}' on host '{Host}' is in stopped status. starting service", service.Name, service.Host);
            controller.Start();
            controller.WaitForStatus(ServiceControllerStatus.Running, service.StartTimeout);
            controller.Refresh();
            status = controller.Status;
        }

        if (status == ServiceControllerStatus.Paused)
        {
            Logger.LogWarning("service '{Name}' on host '{Host}' is in paused status. continue service", service.Name, service.Host);
            controller.Continue();
            controller.WaitForStatus(ServiceControllerStatus.Running, service.StartTimeout);
            controller.Refresh();
            status = controller.Status;
        }

        if (status == ServiceControllerStatus.Running)
        {
            Logger.LogInformation("service '{Name}' on host '{Host}' is in running status", service.Name, service.Host);
            IncreaseEffectedRows();
        }
        else
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
        ValidateGreaterThen(service.StartTimeout, TimeSpan.FromSeconds(5), "start timeout", section);
        ValidateLessThen(service.StartTimeout, TimeSpan.FromMinutes(20), "start timeout", section);
        ValidateGreaterThen(service.StopTimeout, TimeSpan.FromSeconds(5), "stop timeout", section);
        ValidateLessThen(service.StopTimeout, TimeSpan.FromMinutes(20), "stop timeout", section);
    }

    private static void KillServiceProcess(string serviceName, string host)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new PlatformNotSupportedException("This method is only supported on Windows.");
        }

        var options = new ConnectionOptions { EnablePrivileges = true };
        var scope = new ManagementScope($"\\\\{host}\\root\\cimv2", options);
        scope.Connect();

        var query = new ObjectQuery($"SELECT * FROM Win32_Service WHERE Name='{serviceName}'");
        using var searcher = new ManagementObjectSearcher(scope, query);
        using var results = searcher.Get();

        foreach (var item in results)
        {
            if (item == null) { continue; }

            var processId = (uint)item["ProcessId"];

            // use processid to kill process with taskkill
            try
            {
                var processObjGetOpt = new ObjectGetOptions();
                var processPath = new ManagementPath("Win32_Process");
                using var processClass = new ManagementClass(scope, processPath, processObjGetOpt);
                using var processInParams = processClass.GetMethodParameters("Create");
                processInParams["CommandLine"] = $"cmd /c \"taskkill /f /pid {processId}\"";
                using var outParams = processClass.InvokeMethod("Create", processInParams, null);
                int returnCode = Convert.ToInt32(outParams["returnValue"], CultureInfo.CurrentCulture);
                if (returnCode != 0)
                {
                    Console.WriteLine("Error killing process: " + returnCode);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}