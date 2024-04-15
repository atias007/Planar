using Common;
using Microsoft.Extensions.Configuration;

namespace RabbitMQCheck;

internal class HealthCheck(IConfigurationSection section) : BaseDefault(section), ICheckElemnt
{
    public bool? ClusterAlarm { get; private set; } = section.GetValue<bool?>("cluster alarm");
    public bool? LocalAlarm { get; private set; } = section.GetValue<bool?>("local alarm");
    public bool? NodeMirrorSync { get; private set; } = section.GetValue<bool?>("node mirror sync");
    public bool? NodeQuorumCritical { get; private set; } = section.GetValue<bool?>("node quorum critical");
    public string Key => "health check";
}