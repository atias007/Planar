using Common;
using Microsoft.Extensions.Configuration;
using static System.Collections.Specialized.BitVector32;

namespace RabbitMQCheck;

internal class HealthCheck : BaseDefault, ICheckElement
{
    public HealthCheck(IConfigurationSection section, Defaults defaults) : base(section, defaults)
    {
        ClusterAlarm = section.GetValue<bool?>("cluster alarm");
        LocalAlarm = section.GetValue<bool?>("local alarm");
        NodeMirrorSync = section.GetValue<bool?>("node mirror sync");
        NodeQuorumCritical = section.GetValue<bool?>("node quorum critical");
        VirtualHosts = section.GetValue<bool?>("virtual hosts");

        Host = null;
        Key = "[health-check]";
    }

    public HealthCheck(HealthCheck source, string host) : base(source)
    {
        ClusterAlarm = source.ClusterAlarm;
        LocalAlarm = source.LocalAlarm;
        NodeMirrorSync = source.NodeMirrorSync;
        NodeQuorumCritical = source.NodeQuorumCritical;
        VirtualHosts = source.VirtualHosts;

        Host = host;
        Key = $"[health-check: {host}]";
    }

    public bool? ClusterAlarm { get; private set; }
    public bool? LocalAlarm { get; private set; }
    public bool? NodeMirrorSync { get; private set; }
    public bool? NodeQuorumCritical { get; private set; }
    public bool? VirtualHosts { get; private set; }
    public string Key { get; private set; }
    public string? Host { get; private set; }

    public bool IsValid =>
        ClusterAlarm.GetValueOrDefault() ||
        LocalAlarm.GetValueOrDefault() ||
        NodeMirrorSync.GetValueOrDefault() ||
        VirtualHosts.GetValueOrDefault() ||
        NodeQuorumCritical.GetValueOrDefault();
}