using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Planar;
using RestSharp;
using System;
using System.IO;
using System.Threading.Tasks;

namespace BankOfIsraelCurrency
{
    public class CurrencyLoader : BaseJob
    {
        #region Planar Methods

        public override void Configure(IConfigurationBuilder configurationBuilder)
        {
            //// Do Nothig ////
        }

        public override Task ExecuteJob(IJobExecutionContext context)
        {
            //// Execute Job ////
            return SaveCurrency();
        }

        public override void RegisterServices(IServiceCollection services)
        {
            //// Do Nothig ////
        }

        #endregion Planar Methods

        private static async Task SaveCurrency()
        {
            var client = new RestClient("https://www.boi.org.il");
            var request = new RestRequest("currency.xml", Method.Get);
            var response = await client.ExecuteAsync<CURRENCIES>(request);
            if (response.IsSuccessful)
            {
                await File.WriteAllTextAsync(@$"c:\temp\CURRENCIES_{DateTime.Now:ddMMyyyyHHmmss}.xml", response.Content);
            }
            else
            {
                throw new ApplicationException($"Requst to bank of israel return error. StatusCode: {response.StatusCode}");
            }
        }
    }
}