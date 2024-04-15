using Microsoft.Extensions.Configuration;

namespace RabbitMQCheck;

internal class Server(IConfigurationSection section)
{
    public IEnumerable<string> ApiUrls { get; private set; } = (section.Get<string>()?.Split(',')) ?? [];
    public string? Username { get; private set; } = section.GetValue<string>("username");
    public string? Password { get; private set; } = section.GetValue<string>("password");
}