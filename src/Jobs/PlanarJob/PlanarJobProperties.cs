using CommonJob;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Planar;

public class PlanarJobProperties : IFileJobProperties
{
    [YamlMember(Alias = "invoke method")]
    public string InvokeMethod { get; set; } = null!;

    [YamlMember(Alias = "process")]
    public PlanarJobProcessProperties? Process { get; set; }

    [YamlMember(Alias = "rabbitmq")]
    public PlanarJobRabbitMqProperties? RabbitMq { get; set; }

    [YamlMember(Alias = "redis")]
    public PlanarJobRedisProperties? Redis { get; set; }

    [YamlMember(Alias = "http")]
    public PlanarJobHttpProperties? Http { get; set; }

    [YamlIgnore]
    public string Filename => Process?.Filename ?? string.Empty;

    [YamlIgnore]
    public string? Domain => Process?.Domain;

    [YamlIgnore]
    public string? Password => Process?.Password;

    [YamlIgnore]
    public string? UserName => Process?.UserName;

    [YamlIgnore]
    public string Path => Process?.Path ?? string.Empty;

    [YamlIgnore]
    public IEnumerable<string> Files => Process?.Files ?? [];
}

public class PlanarJobHttpProperties
{
    [YamlMember(Alias = "base url")]
    public string BaseUrl { get; set; } = null!;

    [YamlMember(Alias = "route")]
    public string Route { get; set; } = null!;
}

public class PlanarJobRedisProperties
{
    [YamlMember(Alias = "stream name")]
    public string StreamName { get; set; } = null!;

    [YamlMember(Alias = "consumer group")]
    public string ConsumerGroup { get; set; } = null!;
}

public class PlanarJobRabbitMqProperties
{
    [YamlMember(Alias = "exchange")]
    public string Exchange { get; set; } = null!;

    [YamlMember(Alias = "routing key")]
    public string RoutingKey { get; set; } = null!;
}

public class PlanarJobProcessProperties : BaseProcessJobProperties, IFileJobProperties
{
    public string Path { get; set; } = string.Empty;

    public string Filename { get; set; } = null!;

    [YamlIgnore]
    public IEnumerable<string> Files
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Filename) && string.IsNullOrWhiteSpace(Path))
            {
                return [];
            }

            return
            [
                string.IsNullOrWhiteSpace(Path) ? Filename : System.IO.Path.Combine(Path, Filename)
            ];
        }
    }
}