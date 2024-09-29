namespace Common;

using Microsoft.Extensions.DependencyInjection;

public static class ServicesCollectionExtentions
{
    public static IServiceCollection RegisterBaseCheck(this IServiceCollection services)
    {
        services.AddSingleton<CheckSpanTracker>();
        return services;
    }
}