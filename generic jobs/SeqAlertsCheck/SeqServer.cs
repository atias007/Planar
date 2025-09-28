using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net.Http.Json;

namespace SeqAlertsCheck;

internal class SeqServer
{
    public string Url { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
    public bool IsEmpty => string.IsNullOrWhiteSpace(Url);

    public SeqServer()
    {
    }

    public SeqServer(IConfiguration configuration)
    {
        Url = configuration.GetSection("server:url").Value ?? string.Empty;
        ApiKey = configuration.GetSection("server:api key").Value ?? string.Empty;
    }

    public async Task<IEnumerable<AlertState>> GetAlerts()
    {
        using var client = new HttpClient();
        client.BaseAddress = new Uri(Url);
        const string path = "/api/alertstate";
        if (!string.IsNullOrWhiteSpace(ApiKey))
        {
            client.DefaultRequestHeaders.Add("X-Seq-ApiKey", ApiKey);
        }

        var response = await client.GetAsync(path);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<IEnumerable<AlertState>>() ?? [];
        return result;
    }
}