using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Planar.Common;
using Planar.Common.Monitor;
using Planar.Service.Data;
using Quartz;
using System;

namespace Planar.Service;

internal static class DbFactory
{
    public static void QuartzUsePersistentStore(SchedulerBuilder.PersistentStoreOptions configure)
    {
        switch (AppSettings.Database.ProviderName)
        {
            case DbProviders.SqlServer:
                configure.UseSqlServer(AppSettings.Database.ConnectionString ?? string.Empty);
                break;

            case DbProviders.Sqlite:
                configure.UseMicrosoftSQLite(AppSettings.Database.ConnectionString ?? string.Empty);
                break;

            default:
                throw new NotImplementedException($"Database provider {AppSettings.Database.Provider} is not supported");
        }
    }

    public static IServiceCollection AddPlanarDbContext(this IServiceCollection services)
    {
        switch (AppSettings.Database.ProviderName)
        {
            case DbProviders.SqlServer:
                services.AddDbContext<PlanarContext>(o => o.UseSqlServer(
                     AppSettings.Database.ConnectionString,
                     options => options.EnableRetryOnFailure(12, TimeSpan.FromSeconds(5), null)),
                 contextLifetime: ServiceLifetime.Transient,
                 optionsLifetime: ServiceLifetime.Singleton);
                break;

            case DbProviders.Sqlite:
                services.AddDbContext<PlanarContext>(o => o.UseSqlite(AppSettings.Database.ConnectionString),
                    contextLifetime: ServiceLifetime.Transient,
                    optionsLifetime: ServiceLifetime.Singleton);

                services.AddDbContext<PlanarTraceContext>(o => o.UseSqlite(AppSettings.Database.ConnectionString),
                    contextLifetime: ServiceLifetime.Transient,
                    optionsLifetime: ServiceLifetime.Singleton);
                break;
        }

        return services;
    }

    public static IServiceCollection AddPlanarMonitorDataLayers(this IServiceCollection services)
    {
        switch (AppSettings.Database.ProviderName)
        {
            case DbProviders.SqlServer:
                services.AddTransientWithLazy<IMonitorData, MonitorDataSqlServer>();
                break;

            case DbProviders.Sqlite:
                services.AddTransientWithLazy<IMonitorData, MonitorDataSqlite>();
                break;
        }

        return services;
    }

    public static IServiceCollection AddPlanarDataLayers(this IServiceCollection services)
    {
        services.AddPlanarMonitorDataLayers();

        switch (AppSettings.Database.ProviderName)
        {
            case DbProviders.SqlServer:
                services.AddTransientWithLazy<IUserData, UserDataSqlServer>();
                services.AddTransientWithLazy<IGroupData, GroupDataSqlServer>();
                services.AddTransientWithLazy<IAutoMapperData, AutoMapperDataSqlServer>();
                services.AddTransientWithLazy<IConfigData, ConfigDataSqlServer>();
                services.AddTransientWithLazy<IClusterData, ClusterDataSqlServer>();
                services.AddTransientWithLazy<IHistoryData, HistoryDataSqlServer>();
                services.AddTransientWithLazy<ITraceData, TraceDataSqlServer>();
                services.AddTransientWithLazy<IServiceData, ServiceDataSqlServer>();
                services.AddTransientWithLazy<IMetricsData, MetricsDataSqlServer>();
                services.AddTransientWithLazy<IJobData, JobDataSqlServer>();
                services.AddTransient<IJobPropertyDataLayer, JobDataSqlServer>();
                services.AddTransient<IGroupDataLayer, GroupDataSqlServer>();
                services.AddTransient<IMonitorDurationDataLayer, MonitorDataSqlServer>();
                break;

            case DbProviders.Sqlite:
                services.AddTransientWithLazy<IUserData, UserDataSqlite>();
                services.AddTransientWithLazy<IGroupData, GroupDataSqlite>();
                services.AddTransientWithLazy<IAutoMapperData, AutoMapperDataSqlite>();
                services.AddTransientWithLazy<IConfigData, ConfigDataSqlite>();
                services.AddTransientWithLazy<IClusterData, ClusterDataSqlite>();
                services.AddTransientWithLazy<IHistoryData, HistoryDataSqlite>();
                services.AddTransientWithLazy<ITraceData, TraceDataSqlite>();
                services.AddTransientWithLazy<IServiceData, ServiceDataSqlite>();
                services.AddTransientWithLazy<IMetricsData, MetricsDataSqlite>();
                services.AddTransientWithLazy<IJobData, JobDataSqlite>();
                services.AddTransient<IJobPropertyDataLayer, JobDataSqlite>();
                services.AddTransient<IGroupDataLayer, GroupDataSqlite>();
                services.AddTransient<IMonitorDurationDataLayer, MonitorDataSqlite>();
                break;
        }

        return services;
    }
}