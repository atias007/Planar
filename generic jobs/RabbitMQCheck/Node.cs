using Common;
using Microsoft.Extensions.Configuration;

namespace RabbitMQCheck;

internal class Node(IConfigurationSection section, Defaults defaults) : BaseDefault(section, defaults), ICheckElement
{
    public bool? MemoryAlarm { get; private set; } = section.GetValue<bool?>("memory alarm");
    public bool? DiskFreeAlarm { get; private set; } = section.GetValue<bool?>("disk free alarm");
    public bool Active { get; private set; } = section.GetValue<bool?>("active") ?? true;
    public bool IsValid => MemoryAlarm.GetValueOrDefault() || DiskFreeAlarm.GetValueOrDefault();

    public string Key => "[nodes]";
}