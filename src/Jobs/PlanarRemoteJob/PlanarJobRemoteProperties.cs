using YamlDotNet.Serialization;

namespace PlanarRemoteJob;

public class PlanarJobRemoteProperties
{
    [YamlMember(Alias = "host type")]
    public string HostType { get; set; } = string.Empty;

    public RabbitMqJobProperties? RabbitMq { get; set; }

    public RedisJobProperties? Redis { get; set; }

    public HttpJobProperties? Http { get; set; }
}

public class HttpJobProperties
{
    [YamlMember(Alias = "url")]
    public string Url { get; set; } = string.Empty;
}

public class RabbitMqJobProperties
{
    [YamlMember(Alias = "exchange name")]
    public string ExchangeName { get; set; } = string.Empty;

    [YamlMember(Alias = "routing key")]
    public int RoutingKey { get; set; }

    [YamlMember(Alias = "virtual host")]
    public string VirtualHost { get; set; } = string.Empty;
}

public class RedisJobProperties
{
    [YamlMember(Alias = "stream name")]
    public string StreamName { get; set; } = string.Empty;

    [YamlMember(Alias = "consumer group")]
    public string ConsumerGroup { get; set; } = string.Empty;
}