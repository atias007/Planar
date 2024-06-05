using CommonJob;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Planar.Common;
using Planar.Common.Monitor;
using Planar.Service.API;
using Planar.Service.API.Helpers;
using Planar.Service.Audit;
using Planar.Service.Data;
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
            services.AddScoped<ConfigDomain>();
            services.AddScoped<ServiceDomain>();
            services.AddScoped<MonitorDomain>();
            services.AddScoped<ReportDomain>();
            services.AddScoped<TraceDomain>();
            services.AddScoped<TriggerDomain>();
            services.AddScoped<UserDomain>();
            services.AddScoped<ClusterDomain>();
            services.AddScoped<MetricsDomain>();

            // Utils
            services.AddScoped<ClusterUtil>();
            services.AddScoped<MonitorUtil>();
            services.AddScoped<IMonitorUtil>(p => p.GetRequiredService<MonitorUtil>());
            services.AddScoped<JobMonitorUtil>();

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

            services.AddHostedService<PlanarRestartService>();

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
            services.AddAutoMapperProfiles(new[] { assemply });

            return services;
        }

        internal static IServiceCollection AddPlanarDataLayerWithContext(this IServiceCollection services)
        {
            services.AddPlanarDbContext();

            services.AddTransientWithLazy<UserData>();
            services.AddTransientWithLazy<GroupData>();
            services.AddTransientWithLazy<AutoMapperData>();
            services.AddTransientWithLazy<MonitorData>();
            services.AddTransientWithLazy<ConfigData>();
            services.AddTransientWithLazy<ClusterData>();
            services.AddTransientWithLazy<HistoryData>();
            services.AddTransientWithLazy<TraceData>();
            services.AddTransientWithLazy<ServiceData>();
            services.AddTransientWithLazy<MetricsData>();
            services.AddTransientWithLazy<ReportData>();
            services.AddTransientWithLazy<JobData>();
            services.AddTransient<IJobPropertyDataLayer, JobData>();
            services.AddTransient<IMonitorDurationDataLayer, MonitorData>();

            return services;
        }

        internal static IServiceCollection AddTransientWithLazy<T>(this IServiceCollection services)
            where T : BaseDataLayer
        {
            services.AddTransient<T>();
            services.AddTransient(p => new Lazy<T>(() => p.GetRequiredService<T>()));
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

            // Utils
            services.AddScoped<ClusterUtil>();
            services.AddScoped<MonitorUtil>();
            return services;
        }

        internal static IServiceCollection AddPlanarDbContext(this IServiceCollection services)
        {
            services.AddDbContext<PlanarContext>(o => o.UseSqlServer(
                    AppSettings.Database.ConnectionString,
                    options => options.EnableRetryOnFailure(12, TimeSpan.FromSeconds(5), null)),
                contextLifetime: ServiceLifetime.Transient,
                optionsLifetime: ServiceLifetime.Singleton
            );

            return services;
        }
    }
}