using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BankOfIsraelCurrency
{
    public class CurrencyLoader : BaseJob
    {
        #region Planar Methods

        public override void Configure(IConfigurationBuilder configurationBuilder, string environment)
        {
            //// Do Nothig ////
        }

        public override async Task ExecuteJob(IJobExecutionContext context)
        {
            //// Execute Job ////
            await SaveCurrency();
        }

        public override void RegisterServices(IConfiguration configuration, IServiceCollection services)
        {
            //// Do Nothig ////
        }

        #endregion Planar Methods

        private async Task SaveCurrency()
        {
            var client = new RestClient("https://www.boi.org.il");
            var request = new RestRequest("PublicApi/GetExchangeRates", Method.Get);

            Logger.LogInformation("Call bank of israel at: {Uri}", client.BuildUri(request));
            var response = await client.ExecuteAsync<Currencies>(request);
            if (response.IsSuccessful)
            {
                var counter = 0;
                var data = response.Data.ExchangeRates;
                foreach (var item in data)
                {
                    FailOnStopRequest();
                    UpdateProgress(counter, data.Length);
                    Logger.LogInformation(" [x] Handle currency {Currency} with value {Value}", item.Key, item.CurrentExchangeRate);
                    IncreaseEffectedRows();
                    await Task.Delay(3000);
                    counter++;
                }
            }
            else
            {
                throw new ApplicationException($"Requst to bank of israel return error. StatusCode: {response.StatusCode}");
            }
        }
    }
}