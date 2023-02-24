using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Planar.Common;
using Planar.Service.API;
using Planar.Service.API.Helpers;
using Planar.Service.Data;
using Planar.Service.General;
using Planar.Service.Monitor;
using Quartz;
using System;
using System.Reflection;

namespace Planar.Service
{
    public static class ServiceConfiguration
    {
        public static IServiceCollection AddPlanarServices(this IServiceCollection services)
        {
            // Quartz
            services.AddQuartzService();

            // Main Service
            services.AddTransient<MainService>();

            // Domain layers
            services.AddScoped<GroupDomain>();
            services.AddScoped<HistoryDomain>();
            services.AddScoped<JobDomain>();
            services.AddScoped<ConfigDomain>();
            services.AddScoped<ServiceDomain>();
            services.AddScoped<MonitorDomain>();
            services.AddScoped<TraceDomain>();
            services.AddScoped<TriggerDomain>();
            services.AddScoped<UserDomain>();
            services.AddScoped<ClusterDomain>();

            // Utils
            services.AddScoped<ClusterUtil>();
            services.AddScoped<MonitorUtil>();

            // AutoMapper
            services.AddAutoMapper(Assembly.Load($"{nameof(Planar)}.{nameof(Service)}"));

            // DAL
            services.AddPlanarDataLayerWithContext();

            // Service
            services.AddTransient<MainService>();

            // Domains
            services.AddScoped<GroupDomain>();
            services.AddScoped<HistoryDomain>();
            services.AddScoped<JobDomain>();
            services.AddScoped<ConfigDomain>();
            services.AddScoped<ServiceDomain>();
            services.AddScoped<MonitorDomain>();
            services.AddScoped<TraceDomain>();
            services.AddScoped<TriggerDomain>();
            services.AddScoped<UserDomain>();
            services.AddScoped<ClusterDomain>();

            // Scheduler
            services.AddSingleton(p => p.GetRequiredService<ISchedulerFactory>().GetScheduler().Result);
            services.AddSingleton<SchedulerUtil>();
            services.AddSingleton<JobKeyHelper>();
            services.AddSingleton<TriggerKeyHelper>();

            // Host
            services.AddHostedService<MainService>();

            // AutoMapper
            var assemply = Assembly.Load($"{nameof(Planar)}.{nameof(Service)}");
            services.AddAutoMapperProfiles(new[] { assemply });

            return services;
        }

        internal static IServiceCollection AddPlanarDataLayerWithContext(this IServiceCollection services)
        {
            services.AddPlanarDbContext();

            services.AddTransient<UserData>();
            services.AddTransient<GroupData>();
            services.AddTransient<MonitorData>();
            services.AddTransient<ConfigData>();
            services.AddTransient<ClusterData>();
            services.AddTransient<HistoryData>();
            services.AddTransient<TraceData>();
            services.AddTransient<ServiceData>();
            services.AddTransient<StatisticsData>();
            services.AddTransient<JobData>();
            services.AddTransient<IJobPropertyDataLayer, JobData>();

            return services;
        }

        internal static IServiceCollection AddPlanarMonitorServices(this IServiceCollection services)
        {
            services.AddPlanarDbContext();
            services.AddTransient<MonitorData>();

            // Scheduler
            services.AddSingleton(p => p.GetRequiredService<ISchedulerFactory>().GetScheduler().Result);
            services.AddSingleton<SchedulerUtil>();
            services.AddSingleton<JobKeyHelper>();
            services.AddSingleton<TriggerKeyHelper>();

            // Utils
            services.AddScoped<ClusterUtil>();
            services.AddScoped<MonitorUtil>();
            return services;
        }

        private static IServiceCollection AddPlanarDbContext(this IServiceCollection services)
        {
            services.AddDbContext<PlanarContext>(o => o.UseSqlServer(
                    AppSettings.DatabaseConnectionString,
                    options => options.EnableRetryOnFailure(12, TimeSpan.FromSeconds(5), null)),
                contextLifetime: ServiceLifetime.Transient,
                optionsLifetime: ServiceLifetime.Singleton
            );

            return services;
        }
    }
}