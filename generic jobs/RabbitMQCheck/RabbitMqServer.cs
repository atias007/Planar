namespace RabbitMQCheck;

internal class RabbitMqServer
{
    public List<string> Hosts { get; private set; } = new List<string>();
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool IsEmpty => string.IsNullOrWhiteSpace(Username) && string.IsNullOrWhiteSpace(Password) && Hosts.Count == 0;
}
