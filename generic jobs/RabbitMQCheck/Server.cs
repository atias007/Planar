using Common;
using Microsoft.Extensions.Configuration;

namespace RabbitMQCheck;

internal class Server
{
    public Server(IConfiguration configuration)
    {
        var section = configuration.GetRequiredSection(Consts.RabbitMQConfigSection);
        Hosts = section.GetSection("hosts").Get<string[]>() ?? [];
        Username = section.GetValue<string>("username") ?? string.Empty;
        Password = section.GetValue<string>("password") ?? string.Empty;
    }

    public IEnumerable<string> Hosts { get; private set; }
    public string Username { get; private set; }
    public string Password { get; private set; }

    public string DefaultHost => Hosts.FirstOrDefault() ?? string.Empty;
}