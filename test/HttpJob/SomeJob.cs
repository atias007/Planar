using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Job;
using Serilog;

namespace HttpJob
{
    internal class SomeJob : BaseJob
    {
        public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
        {
        }

        public override async Task ExecuteJob(IJobExecutionContext context)
        {
            var dal = ServiceProvider.GetRequiredService<IDataLayer>();
            var currencies = dal.GetCurrency();
            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.LogDebug("Currency count: {Count}", currencies.Count());
            }
            
            var total = currencies.Count();
            var current = 0;
            foreach (var item in currencies)
            {
                if (Logger.IsEnabled(LogLevel.Information))
                {
                    Logger.LogInformation("{Name}: {Rate:N4}", item.Name, item.Rate);
                }

                await IncreaseEffectedRowsAsync();
                current++;
                await base.UpdateProgressAsync(current, total);
                await Task.Delay(3000);
            }
        }

        public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
        {
            services.AddScoped<IDataLayer, DataLayer>();
        }
    }

    internal interface IDataLayer
    {
        IEnumerable<Currency> GetCurrency();
    }

    internal class DataLayer : IDataLayer
    {
        public IEnumerable<Currency> GetCurrency()
        {
            return
            [
                new() { Name = "USD", Rate = 1.0 },
                new() { Name = "EUR", Rate = 0.85 },
                new() { Name = "JPY", Rate = 110.0 }
            ];
        }
    }

    internal class Currency
    {
        public required string Name { get; set; }
        public required double Rate { get; set; }
    }
}