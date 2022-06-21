using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BankOfIsraelCurrency
{
    public class CurrencyLoader : BaseJob
    {
        #region Planar Methods

        public override void Configure(IConfigurationBuilder configurationBuilder)
        {
            //// Do Nothig ////
        }

        public override async Task ExecuteJob(IJobExecutionContext context)
        {
            //// Execute Job ////
            var t1 = SaveCurrency();
            var t2 = SaveCurrencyV2();
            await Task.WhenAll(t1, t2);
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

        private async Task SaveCurrencyV2()
        {
            var client = new RestClient("https://www.boi.org.il");
            var request = new RestRequest("currency.xml", Method.Get);

            Logger.LogInformation("Call bank of israel at: {Uri}", client.BuildUri(request));
            var response = await client.ExecuteAsync<CURRENCIES>(request);
            if (response.IsSuccessful)
            {
                var counter = 0;
                var data = Deserialize(response.Content);
                foreach (var item in data)
                {
                    UpdateProgress(counter, data.Count());
                    Logger.LogInformation(" [x] Handle currency {Currency} with value {Value}", item.NAME, item.RATE);
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

        private static IEnumerable<CURRENCIESCURRENCY> Deserialize(string xml)
        {
            var doc = XDocument.Parse(xml);
            var elements = doc.Element("CURRENCIES").Elements("CURRENCY");
            foreach (var element in elements)
            {
                yield return new CURRENCIESCURRENCY
                {
                    NAME = element.Element("NAME").Value,
                    RATE = decimal.Parse(element.Element("RATE").Value),
                };
            }
        }
    }
}