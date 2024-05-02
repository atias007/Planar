using Microsoft.Extensions.Configuration;

namespace RabbitMQCheck;

internal class Server(IConfigurationSection section)
{
    public IEnumerable<string> Hosts { get; private set; } = (section.GetValue<string>("hosts")?.Split(',')) ?? [];
    public string Username { get; private set; } = section.GetValue<string>("username") ?? string.Empty;
    public string Password { get; private set; } = section.GetValue<string>("password") ?? string.Empty;

    public string DefaultHost => Hosts.First();
}