using CommonJob;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Planar;

public class PlanarJobProperties : IFileJobProperties
{
    [YamlMember(Alias = "invoke method", Order = 0)]
    public string InvokeMethod { get; set; } = null!;

    [YamlMember(Alias = "process", Order = 1)]
    public PlanarJobProcessProperties? Process { get; set; }

    [YamlMember(Alias = "rabbitmq", Order = 2)]
    public PlanarJobRabbitMqProperties? RabbitMq { get; set; }

    [YamlMember(Alias = "redis", Order = 3)]
    public PlanarJobRedisProperties? Redis { get; set; }

    [YamlMember(Alias = "http", Order = 4)]
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
    [YamlMember(Alias = "base url", Order = 0)]
    public string BaseUrl { get; set; } = null!;

    [YamlMember(Alias = "route", Order = 1)]
    public string Route { get; set; } = null!;
}

public class PlanarJobRedisProperties
{
    [YamlMember(Alias = "stream name", Order = 0)]
    public string StreamName { get; set; } = null!;

    [YamlMember(Alias = "consumer group", Order = 1)]
    public string ConsumerGroup { get; set; } = null!;
}

public class PlanarJobRabbitMqProperties
{
    [YamlMember(Alias = "exchange", Order = 0)]
    public string Exchange { get; set; } = null!;

    [YamlMember(Alias = "routing key", Order = 1)]
    public string RoutingKey { get; set; } = null!;
}

public class PlanarJobProcessProperties : BaseProcessJobProperties, IFileJobProperties
{
    public string Path { get; set; } = string.Empty;

    [YamlMember(Alias = "filename", Order = 0)]
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