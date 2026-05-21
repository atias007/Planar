using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Channels;

namespace Planar.Common.PeriodicalBatch;

public static class PeriodicalBatchExtentions

{
    public static IServiceCollection AddPeriodicalBatchService<TService, TMessage>(this IServiceCollection services)
                where TService : PeriodicalBatch<TMessage>
                where TMessage : class

    {
        services.AddHostedService<TService>();
        services.AddSingleton<TService>();
        services.AddSingleton<PeriodicalBatchProducer<TMessage>>();
        services.AddSingleton(Channel.CreateUnbounded<TMessage>());
        services.AddSingleton(PeriodicalBatchOptions<TMessage>.Empty);
        return services;
    }

    public static IServiceCollection AddPeriodicalBatchService<TService, TMessage>(this IServiceCollection services, Action<PeriodicalBatchOptionsBuilder<TMessage>> options)
                where TService : PeriodicalBatch<TMessage>
                where TMessage : class
    {
        services.AddHostedService<TService>();
        services.AddSingleton<TService>();
        services.AddSingleton<PeriodicalBatchProducer<TMessage>>();
        services.AddSingleton(Channel.CreateUnbounded<TMessage>());
        services.AddSingleton<PeriodicalBatchOptions<TMessage>>(p =>
                {
                    var builder = new PeriodicalBatchOptionsBuilder<TMessage>();
                    options(builder);
                    return builder.Build();
                });

        return services;
    }
}