using Common;
using Microsoft.Extensions.Configuration;

namespace RabbitMQCheck;

internal class HealthCheck(IConfigurationSection section, Defaults defaults) : BaseDefault(section, defaults), ICheckElement
{
    public bool? ClusterAlarm { get; private set; } = section.GetValue<bool?>("cluster alarm");
    public bool? LocalAlarm { get; private set; } = section.GetValue<bool?>("local alarm");
    public bool? NodeMirrorSync { get; private set; } = section.GetValue<bool?>("node mirror sync");
    public bool? NodeQuorumCritical { get; private set; } = section.GetValue<bool?>("node quorum critical");
    public string Key => "[health-check]";
    public bool Active { get; private set; } = section.GetValue<bool?>("active") ?? true;

    public bool IsValid =>
        ClusterAlarm.GetValueOrDefault() ||
        LocalAlarm.GetValueOrDefault() ||
        NodeMirrorSync.GetValueOrDefault() ||
        NodeQuorumCritical.GetValueOrDefault();
}

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
internal record HealthCheckExtended(Server Server, string Host);

#pragma warning restore SA1313 // Parameter names should begin with lower-case letter