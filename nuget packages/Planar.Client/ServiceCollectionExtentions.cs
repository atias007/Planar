using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Planar.Client
{
    public static class ServiceCollectionExtentions
    {
        public static ServiceCollection AddPlanarClient(this ServiceCollection services)
        {
            services.AddPlanarClient(o =>
            {
                o.Host = "http://localhost:2306";
            });

            return services;
        }

        public static ServiceCollection AddPlanarClient(this ServiceCollection services, Action<PlanarClientConnectOptions> options)
        {
            var connectOptions = new PlanarClientConnectOptions();
            options(connectOptions);
            services.TryAddSingleton<IPlanarClient>(p =>
            {
                var client = new PlanarClient();
                client.ConnectAsync(connectOptions).Wait();
                return client;
            });

            return services;
        }
    }
}