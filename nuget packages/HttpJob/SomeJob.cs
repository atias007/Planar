using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Job;

namespace HttpJob
{
    internal class SomeJob : BaseJob
    {
        public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
        {
        }

        public override async Task ExecuteJob(IJobExecutionContext context)
        {
            var dal = ServiceProvider.GetRequiredService<DataLayer>();
            var currencies = dal.GetCurrency();
            Logger.LogDebug("Currency count: {Count}", currencies.Count());
            var total = currencies.Count();
            var current = 0;
            foreach (var item in currencies)
            {
                Logger.LogInformation($"{item.Name}: {item.Rate:N4}");
                await IncreaseEffectedRowsAsync();
                current++;
                await base.UpdateProgressAsync(current, total);
                await Task.Delay(3000);
            }
        }

        public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
        {
            services.AddScoped<DataLayer>();
        }
    }

    internal class DataLayer
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