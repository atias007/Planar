using InfluxDB.Client;
using InfluxDB.Client.Core;
using InfluxDB.Client.Core.Flux.Domain;

namespace InfluxDBCheck
{
    internal class InfluxProxy
    {
        private readonly IInfluxDBClient _client;

        public InfluxProxy(Server server)
        {
            var options = new InfluxDBClientOptions(server.Url)
            {
                LogLevel = LogLevel.Body,
                Org = server.Organization,
                Token = server.Token,
            };

            _client = new InfluxDBClient(options);
        }

        public async Task<List<FluxTable>> QueryAsync(InfluxQuery query)
        {
            using var ts = new CancellationTokenSource(query.Timeout);

            var api = _client.GetQueryApi();
            var table = await api.QueryAsync(query.Query, cancellationToken: ts.Token);
            return table;
        }
    }
}