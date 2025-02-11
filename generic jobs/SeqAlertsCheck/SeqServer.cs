using Microsoft.Extensions.Configuration;
using Seq.Api;
using Seq.Api.Model.Alerting;

namespace SeqAlertsCheck;

internal class SeqServer
{
    public string Url { get; set; } = string.Empty; //
    public string? ApiKey { get; set; } //
    public bool IsEmpty => string.IsNullOrWhiteSpace(Url);

    public SeqServer()
    {
    }

    public SeqServer(IConfiguration configuration)
    {
        Url = configuration.GetSection("server:url").Value ?? string.Empty;
        ApiKey = configuration.GetSection("server:api key").Value ?? string.Empty;
    }

    public async Task<IEnumerable<AlertStateEntity>> GetAlerts()
    {
        var connection =
            string.IsNullOrWhiteSpace(ApiKey) ?
            new SeqConnection(Url) :
            new SeqConnection(Url, ApiKey);

        var result = await connection.AlertState.ListAsync();
        return result;
    }
}