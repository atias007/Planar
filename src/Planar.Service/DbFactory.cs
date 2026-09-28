using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
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

    public static bool IsDuplicateKey(DbUpdateException ex)
    {
        return ex.InnerException switch
        {
            SqlException { Number: 2627 or 2601 } => true,
            SqliteException { SqliteExtendedErrorCode: 1555 or 2067 } => true,
            _ => false
        };
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
                services.AddScopedWithLazy<IConfigData, ConfigDataSqlServer>();
                services.AddScopedWithLazy<IClusterData, ClusterDataSqlServer>();
                services.AddScopedWithLazy<IHistoryData, HistoryDataSqlServer>();
                services.AddScopedWithLazy<ITraceData, TraceDataSqlServer>();
                services.AddScopedWithLazy<IServiceData, ServiceDataSqlServer>();
                services.AddScopedWithLazy<IMetricsData, MetricsDataSqlServer>();
                services.AddScopedWithLazy<IJobData, JobDataSqlServer>();
                services.AddScopedWithLazy<IResourceData, ResourceDataSqlServer>();
                services.AddScoped<IJobPropertyDataLayer, JobDataSqlServer>();
                services.AddScoped<IGroupDataLayer, GroupDataSqlServer>();
                services.AddScoped<IMonitorDurationDataLayer, MonitorDataSqlServer>();
                break;

            case DbProviders.Sqlite:
                services.AddScopedWithLazy<IUserData, UserDataSqlite>();
                services.AddScopedWithLazy<IGroupData, GroupDataSqlite>();
                services.AddScopedWithLazy<IConfigData, ConfigDataSqlite>();
                services.AddScopedWithLazy<IClusterData, ClusterDataSqlite>();
                services.AddScopedWithLazy<IHistoryData, HistoryDataSqlite>();
                services.AddScopedWithLazy<ITraceData, TraceDataSqlite>();
                services.AddScopedWithLazy<IServiceData, ServiceDataSqlite>();
                services.AddScopedWithLazy<IMetricsData, MetricsDataSqlite>();
                services.AddScopedWithLazy<IJobData, JobDataSqlite>();
                services.AddScopedWithLazy<IResourceData, ResourceDataSqlite>();
                services.AddScoped<IJobPropertyDataLayer, JobDataSqlite>();
                services.AddScoped<IGroupDataLayer, GroupDataSqlite>();
                services.AddScoped<IMonitorDurationDataLayer, MonitorDataSqlite>();
                break;
        }

        return services;
    }
}