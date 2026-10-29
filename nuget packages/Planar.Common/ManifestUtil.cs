using Planar.Common.Proxy;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Job
{
    internal class ManifestUtil
    {
        protected ManifestUtil()
        {
        }

        public static async Task ApplyManifest(PlanarHostedJobStartProperties properties, CancellationToken cancellationToken)
        {
            try
            {
                await ApplyManifestInner(properties, cancellationToken);
            }
            catch (System.Exception ex)
            {
                throw new PlanarJobException($"Fail to apply manifests. {ex.Message}", ex);
            }
        }

        private static async Task ApplyManifestInner(PlanarHostedJobStartProperties properties, CancellationToken cancellationToken)
        {
            if (!properties.Manifests.Any()) { return; }
            var body = string.Join("\r\n---\r\n", properties.Manifests.Select(manifest => manifest.Content));
            var proxy = new RestProxy();
            var loginData = new LoginData
            {
                Host = properties.PlanarHostname,
                Port = properties.PlanarApiPort,
                SecureProtocol = properties.PlanarApiSchema == HttpSchema.Https,
                Username = properties.PlanarApiUsername,
                Password = properties.PlanarApiPassword
            };

            var login = await proxy.Login(loginData, cancellationToken);
            if (!login.IsSuccess && login.StatusCode != System.Net.HttpStatusCode.Conflict)
            {
                throw new PlanarJobException($"Login to Planar api failed with status code: {login.StatusCode}");
            }

            var request = new RestRequest("service/apply", HttpMethod.Post)
                .AddYamlStringBody(body);

            var response = await proxy.InvokeAsync<ApplyResponse>(request, cancellationToken);
            if (!response.IsSuccess)
            {
                await DumpResponse(response);
                throw new PlanarJobException($"Planar api returned status code: {response.StatusCode}");
            }

            var info = response.Data;
            if (info == null)
            {
                await DumpResponse(response);
                throw new PlanarJobException("Planar api returned empty or invalid response");
            }

            DumpResponse(info);
        }

        private static async Task DumpResponse(RestResponse response)
        {
            Console.WriteLine();
            Console.WriteLine("------------------");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"StatusCode: {response.StatusCode}");
            Console.ResetColor();
            Console.WriteLine("------------------");
            Console.WriteLine($"Content: {await response.GetStringContent()}");
            Console.WriteLine("------------------");
        }

        private static void DumpResponse(ApplyResponse response)
        {
            Console.WriteLine();
            Console.WriteLine("----------");
            Console.WriteLine("Summary");
            Console.WriteLine("----------");
            Console.WriteLine($"Total Update:    {FormatNumber(response.TotalUpdate)}");
            Console.WriteLine($"Total Add:       {FormatNumber(response.TotalAdd)}");
            Console.WriteLine($"Total Delete:    {FormatNumber(response.TotalDelete)}");
            Console.WriteLine($"Total Unchanged: {FormatNumber(response.TotalUnchanged)}");
            Console.WriteLine($"Total Errors:    {FormatNumber(response.TotalErrors)}");
            Console.WriteLine("----------");
            Console.WriteLine("Details");
            Console.WriteLine("----------");
            foreach (var item in response.Items)
            {
                Console.WriteLine($"{item.Source} (kind: {item.Kind}) --> {item.Action} --> {item.Description}");
            }

            Console.WriteLine();

            if (response.TotalErrors > 0)
            {
                throw new PlanarJobException("Planar api returned some errors");
            }
        }

        private static string FormatNumber(int number)
        {
            if (number == 0) { return "-"; }
            return number.ToString("N0");
        }
    }
}