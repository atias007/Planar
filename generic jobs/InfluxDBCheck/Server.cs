using Common;
using Microsoft.Extensions.Configuration;

namespace InfluxDBCheck;

internal class Server
{
    public Server(IConfiguration configuration)
    {
        var section = configuration.GetRequiredSection(Consts.InfluxDBConfigSection);
        Url = section.GetValue<string>("host") ?? string.Empty;
        Token = section.GetValue<string>("token") ?? string.Empty;
        Organization = section.GetValue<string>("organization") ?? string.Empty;
    }

    public string Url { get; private set; }
    public string Token { get; private set; }
    public string Organization { get; private set; }
}