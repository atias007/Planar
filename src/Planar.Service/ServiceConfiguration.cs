using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Planar.Service.API;
using Planar.Service.Data;
using Planar.Service.General;
using System;
using System.Reflection;

namespace Planar.Service
{
    public static class ServiceConfiguration
    {
        public static IServiceCollection AddPlanarServices(this IServiceCollection services)
        {
            services.AddAutoMapper(Assembly.Load($"{nameof(Planar)}.{nameof(Service)}"));

            services.AddPlanarDataLayerWithContext();
            services.AddTransient<MainService>();
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

            services.AddHostedService<MainService>();

            return services;
        }

        internal static IServiceCollection AddPlanarDataLayerWithContext(this IServiceCollection services)
        {
            services.AddTransient<DataLayer>();
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