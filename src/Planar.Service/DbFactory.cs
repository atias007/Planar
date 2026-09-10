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
                     options =>
                     {
                         options.EnableRetryOnFailure(4, TimeSpan.FromSeconds(1), errorNumbersToAdd: null);
                         options.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                     }),
                 contextLifetime: ServiceLifetime.Transient,
                 optionsLifetime: ServiceLifetime.Singleton);
                break;

            case DbProviders.Sqlite:
                services.AddDbContext<PlanarContext>(o => o.UseSqlite(AppSettings.Database.ConnectionString,
                    options =>
                    {
                        options.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    }),
                    contextLifetime: ServiceLifetime.Transient,
                    optionsLifetime: ServiceLifetime.Singleton);

                services.AddDbContext<PlanarTraceContext>(o => o.UseSqlite(AppSettings.Database.ConnectionString,
                    options =>
                    {
                        options.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    }),
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
                services.AddScopedWithLazy<IMonitorData, MonitorDataSqlServer>();
                break;

            case DbProviders.Sqlite:
                services.AddScopedWithLazy<IMonitorData, MonitorDataSqlite>();
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
                services.AddScopedWithLazy<IUserData, UserDataSqlServer>();
                services.AddScopedWithLazy<IGroupData, GroupDataSqlServer>();
                services.AddScopedWithLazy<IAutoMapperData, AutoMapperDataSqlServer>();
                services.AddScopedWithLazy<IConfigData, ConfigDataSqlServer>();
                services.AddScopedWithLazy<IClusterData, ClusterDataSqlServer>();
                services.AddScopedWithLazy<IHistoryData, HistoryDataSqlServer>();
                services.AddScopedWithLazy<ITraceData, TraceDataSqlServer>();
                services.AddScopedWithLazy<IServiceData, ServiceDataSqlServer>();
                services.AddScopedWithLazy<IMetricsData, MetricsDataSqlServer>();
                services.AddScopedWithLazy<IJobData, JobDataSqlServer>();
                services.AddScopedWithLazy<IJobPropertyDataLayer, JobDataSqlServer>();
                break;

            case DbProviders.Sqlite:
                services.AddScopedWithLazy<IUserData, UserDataSqlite>();
                services.AddScopedWithLazy<IGroupData, GroupDataSqlite>();
                services.AddScopedWithLazy<IAutoMapperData, AutoMapperDataSqlite>();
                services.AddScopedWithLazy<IConfigData, ConfigDataSqlite>();
                services.AddScopedWithLazy<IClusterData, ClusterDataSqlite>();
                services.AddScopedWithLazy<IHistoryData, HistoryDataSqlite>();
                services.AddScopedWithLazy<ITraceData, TraceDataSqlite>();
                services.AddScopedWithLazy<IServiceData, ServiceDataSqlite>();
                services.AddScopedWithLazy<IMetricsData, MetricsDataSqlite>();
                services.AddScopedWithLazy<IJobData, JobDataSqlite>();
                services.AddScopedWithLazy<IJobPropertyDataLayer, JobDataSqlite>();
                break;
        }

        services.AddScoped<IGroupDataLayer>(p => p.GetRequiredService<IGroupData>());
        services.AddScoped<IMonitorDurationDataLayer>(p => p.GetRequiredService<IMonitorData>());

        return services;
    }
}