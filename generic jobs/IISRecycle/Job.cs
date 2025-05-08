using Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Web.Administration;
using Planar.Job;

namespace IISRecycle;

internal partial class Job : BaseCheckJob
{
#pragma warning disable S3251 // Implementations should be provided for "partial" methods

    partial void CustomConfigure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context);

    partial void VetoApplicationPool(ApplicationPool pool);

    partial void VetoHost(Host host);

    partial void Finalayze(FinalayzeDetails<IEnumerable<ApplicationPool>> details);

#pragma warning restore S3251 // Implementations should be provided for "partial" methods

    public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
        => CustomConfigure(configurationBuilder, context);

    public async override Task ExecuteJob(IJobExecutionContext context)
    {
        Initialize(ServiceProvider);

        var defaults = GetDefaults(Configuration);
        var hosts = GetHosts(Configuration, h => VetoHost(h));
        var pools = GetApplicationPool(Configuration, defaults);
        ValidateRequired(hosts, "hosts");
        pools = GetApplicationPoolsWithHost(pools, hosts);
        EffectedRows = 0;
        await SafeInvokeOperation(pools, InvokeApplicationPoolInner, context.TriggerDetails);

        var details = GetFinalayzeDetails(pools.AsEnumerable());
        Finalayze(details);
        Finalayze();
    }

    private void InvokeApplicationPoolInner(ApplicationPool pool)
    {
        var config = pool.ServerConfigFile ?? "c$\\Windows\\System32\\inetsrv\\Config\\applicationHost.config";
        config = $"\\\\{pool.Host}\\{config}";

        if (!File.Exists(config))
        {
            throw new FileNotFoundException($"configuration file '{config}' not found in host {pool.Host}");
        }

        using var iisManager = new ServerManager(false, config);

        var apppool = iisManager.ApplicationPools.FirstOrDefault(p => string.Equals(p.Name, pool.Name, StringComparison.OrdinalIgnoreCase);
        if (apppool == null)
        {
            throw new InvalidDataException($"application pool '{pool.Name}' not found in host {pool.Host}");
        }

        if (apppool.State != ObjectState.Started)
        {
            Logger.LogWarning("application pool '{Name}' is not started ({State}). cannot recycle", pool.Name, apppool.State);
        }

        var objState = apppool.Recycle();
    }

    public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
    {
        services.RegisterSpanCheck();
    }

    private Defaults GetDefaults(IConfiguration configuration)
    {
        var empty = Defaults.Empty;
        var section = configuration.GetSection("defaults");
        if (section == null)
        {
            Logger.LogWarning("no defaults section found on settings file. set job factory defaults");
            return empty;
        }

        var result = new Defaults(section);
        ValidateBase(result, "defaults");

        return result;
    }

    private List<ApplicationPool> GetApplicationPool(IConfiguration configuration, Defaults defaults)
    {
        var result = new List<ApplicationPool>();
        var pools = configuration.GetRequiredSection("application pools");
        foreach (var item in pools.GetChildren())
        {
            var pool = new ApplicationPool(item, defaults);

            VetoApplicationPool(pool);
            if (CheckVeto(pool, "application pool")) { continue; }

            ValidateApplicationPool(pool);
            result.Add(pool);
        }

        ValidateRequired(result, "application pool");
        ValidateDuplicateNames(result, "application pool");

        return result;
    }

    private static void ValidateApplicationPool(ApplicationPool pool)
    {
        var section = $"application pool ({pool.Name})";

        ValidateRequired(pool.Name, "name", section);
        ValidateMaxLength(pool.Name, 100, "name", section);

        ValidateRequired(pool.HostGroupName, "host group name", section);
    }

    private static List<ApplicationPool> GetApplicationPoolsWithHost(List<ApplicationPool> pools, IReadOnlyDictionary<string, HostsConfig> hosts)
    {
        var result = new List<ApplicationPool>();
        foreach (var pool in pools)
        {
            if (!hosts.TryGetValue(pool.HostGroupName, out var hostGroup))
            {
                throw new InvalidDataException($"application pool '{pool.Name}' has no host group name '{pool.HostGroupName}'");
            }

            foreach (var host in hostGroup.Hosts)
            {
                var clone = new ApplicationPool(pool)
                {
                    Host = host
                };
                result.Add(clone);
            }
        }

        return result;
    }
}