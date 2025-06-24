using CommonJob;
using Microsoft.Extensions.DependencyInjection;
using Planar.Common;
using Planar.Service.API;
using Planar.Service.API.Helpers;
using Planar.Service.Audit;
using Planar.Service.General;
using Planar.Service.Monitor;
using Planar.Service.Services;
using Quartz;
using System;
using System.Reflection;
using System.Threading.Channels;

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
            services.AddScoped<JobDomainSse>();
            services.AddScoped<ConfigDomain>();
            services.AddScoped<ServiceDomain>();
            services.AddScoped<MonitorDomain>();
            services.AddScoped<ReportDomain>();
            services.AddScoped<TraceDomain>();
            services.AddScoped<TriggerDomain>();
            services.AddScoped<UserDomain>();
            services.AddScoped<ClusterDomain>();
            services.AddScoped<MetricsDomain>();

            services.AddScoped<IJobActions, JobDomain>(p => p.GetRequiredService<JobDomain>());

            // Utils
            services.AddSingleton<ClusterUtil>();
            services.AddSingleton<IClusterUtil>(p => p.GetRequiredService<ClusterUtil>());
            services.AddSingleton<MonitorUtil>();
            services.AddSingleton<IMonitorUtil>(p => p.GetRequiredService<MonitorUtil>());
            services.AddSingleton<JobMonitorUtil>();
            services.AddSingleton<SchedulerHealthCheckUtil>();

            // AutoMapper
            services.AddAutoMapper(Assembly.Load($"{nameof(Planar)}.{nameof(Service)}"));

            // DAL
            services.AddPlanarDataLayerWithContext();

            // Service
            services.AddSingleton<MainService>();
            services.AddSingleton<AuditService>();
            services.AddSingleton<MonitorService>();
            services.AddSingleton<SecurityService>();
            services.AddSingleton<MqttBrokerService>();
            services.AddSingleton<MonitorDurationCache>();

            // Scheduler
            services.AddSingleton(p => p.GetRequiredService<ISchedulerFactory>().GetScheduler().Result);
            services.AddSingleton<SchedulerUtil>();
            services.AddSingleton<JobKeyHelper>();

            // Hosted Services
            services.AddHostedService(p => p.GetRequiredService<MainService>());
            services.AddHostedService(p => p.GetRequiredService<AuditService>());
            services.AddHostedService(p => p.GetRequiredService<MonitorService>());
            services.AddHostedService(p => p.GetRequiredService<MqttBrokerService>());
            services.AddHostedService<RestartService>();
            if (AppSettings.Authentication.HasAuthontication)
            {
                services.AddHostedService(p => p.GetRequiredService<SecurityService>());
            }

            // Channel
            services.AddSingleton(Channel.CreateUnbounded<AuditMessage>());
            services.AddSingleton(Channel.CreateUnbounded<SecurityMessage>());
            services.AddSingleton(Channel.CreateUnbounded<MonitorScanMessage>());
            services.AddSingleton<AuditProducer>();
            services.AddSingleton<SecurityProducer>();
            services.AddSingleton<MonitorScanProducer>();

            // AutoMapper
            var assemply = Assembly.Load($"{nameof(Planar)}.{nameof(Service)}");
            services.AddAutoMapperProfiles([assemply]);

            return services;
        }

        internal static IServiceCollection AddPlanarDataLayerWithContext(this IServiceCollection services)
        {
            services.AddPlanarDbContext();
            services.AddPlanarDataLayers();
            return services;
        }

        internal static IServiceCollection AddTransientWithLazy<TService, TImplementation>(this IServiceCollection services)
            where TService : class
            where TImplementation : class, TService
        {
            services.AddTransient<TService, TImplementation>();
            services.AddTransient(p => new Lazy<TService>(() => p.GetRequiredService<TService>()));
            return services;
        }

        ////internal static IServiceCollection AddPlanarMonitorServices(this IServiceCollection services)
        ////{
        ////    services.AddPlanarDbContext();
        ////    services.AddPlanarMonitorDataLayers();

        ////    // Scheduler
        ////    services.AddSingleton(p => p.GetRequiredService<ISchedulerFactory>().GetScheduler().Result);
        ////    services.AddSingleton<SchedulerUtil>();
        ////    services.AddSingleton<JobKeyHelper>();

        ////    // Utils
        ////    services.AddScoped<ClusterUtil>();
        ////    services.AddScoped<MonitorUtil>();
        ////    return services;
        ////}
    }
}