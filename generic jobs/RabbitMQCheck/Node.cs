using Common;
using Microsoft.Extensions.Configuration;
using static System.Collections.Specialized.BitVector32;

namespace RabbitMQCheck;

internal class Node : BaseDefault, ICheckElement
{
    public Node(IConfigurationSection section, Defaults defaults) : base(section, defaults)
    {
        MemoryAlarm = section.GetValue<bool?>("memory alarm");
        DiskFreeAlarm = section.GetValue<bool?>("disk free alarm");
        Key = "[nodes]";
        Host = null;
    }

    public Node(Node source, string host) : base(source)
    {
        MemoryAlarm = source.MemoryAlarm;
        DiskFreeAlarm = source.DiskFreeAlarm;
        Key = $"[nodes: {host}]";
        Host = host;
    }

    public bool? MemoryAlarm { get; private set; }
    public bool? DiskFreeAlarm { get; private set; }
    public bool IsValid => MemoryAlarm.GetValueOrDefault() || DiskFreeAlarm.GetValueOrDefault();
    public string Key { get; private set; }
    public string? Host { get; private set; }

    public IEnumerable<NodeResult> Result { get; set; } = [];
}