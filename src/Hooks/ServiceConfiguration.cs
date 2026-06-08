using Microsoft.Extensions.DependencyInjection;

namespace Planar.Hooks;

public static class ServiceConfiguration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddHooks()
        {
            services.AddSingleton<ISystemHook, PlanarRestHook>();
            services.AddSingleton<ISystemHook, PlanarSmtpHook>();
            services.AddSingleton<ISystemHook, PlanarLogHook>();
            services.AddSingleton<ISystemHook, PlanarTeamsHook>();
            services.AddSingleton<ISystemHook, PlanarTwilioSmsHook>();
            services.AddSingleton<ISystemHook, PlanarRedisStreamHook>();
            services.AddSingleton<ISystemHook, PlanarRedisPubSubHook>();
            services.AddSingleton<ISystemHook, PlanarRabbitMqHook>();
            services.AddSingleton<ISystemHook, PlanarTelegramHook>();

            return services;
        }
    }
}