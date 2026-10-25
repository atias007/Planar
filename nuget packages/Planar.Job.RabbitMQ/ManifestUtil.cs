using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Planar.Job
{
    internal class ManifestUtil
    {
        public static async Task ApplyManifest(PlanarHostedJobStartProperties properties)
        {
            if (!properties.Manifests.Any()) { return; }
            var yaml = string.Join("\r\n---\r\n", properties.Manifests);
            var uri = new Uri($"{properties.PlanarApiSchema}://{properties.PlanarHostname}:{properties.PlanarApiPort}/service/apply");
            using (var client = new HttpClient())
            {
                var content = new StringContent(yaml, System.Text.Encoding.UTF8, "application/x-yaml");
                var response = await client.PostAsync(uri, content);
                response.EnsureSuccessStatusCode();
            }
        }
    }
}