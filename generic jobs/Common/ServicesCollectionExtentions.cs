namespace Common;

using Microsoft.Extensions.DependencyInjection;

public static class ServicesCollectionExtentions
{
    public static IServiceCollection RegisterSpanCheck(this IServiceCollection services)
    {
        services.AddSingleton<CheckSpanTracker>();
        return services;
    }
}