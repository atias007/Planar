using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Job;
using RestSharp;
using System;
using System.Threading.Tasks;

namespace BankOfIsraelCurrency
{
    public class Job : BaseJob
    {
        #region Planar Methods

        public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
        {
            Version = new Version("3.0.0");

            //// Do Nothig ////
        }

        public override async Task ExecuteJob(IJobExecutionContext context)
        {
            Logger.LogWarning("Sample Warning Log");
            await SaveCurrency();
        }

        public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
        {
            //// Do Nothig ////
        }

        #endregion Planar Methods

        private async Task SaveCurrency()
        {
            const string url = "https://www.boi.org.il";
            var client = new RestClient(url);
            var request = new RestRequest("PublicApi/GetExchangeRates", Method.Get);

            Logger.LogInformation("Call bank of israel at: {Uri}", client.BuildUri(request));
            var response = await client.ExecuteAsync<Currencies>(request);
            if (response.IsSuccessful)
            {
                EffectedRows = 0;
                var counter = 0;
                var data = response.Data.ExchangeRates;
                foreach (var item in data)
                {
                    await UpdateProgressAsync(counter, data.Length);
                    Logger.LogInformation(" [x] Handle currency {Currency} with value {Value}", item.Key, item.CurrentExchangeRate);
                    EffectedRows++;
                    await Task.Delay(3000);
                    counter++;
                }
            }
            else
            {
                throw new InvalidOperationException($"Requst to bank of israel return error. StatusCode: {response.StatusCode}");
            }
        }
    }
}