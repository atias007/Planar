using Common;
using Microsoft.Extensions.Configuration;

namespace RabbitMQCheck;

internal class Queue(IConfigurationSection section, Defaults defaults) : BaseDefault(section, defaults), INamedCheckElement, IVetoEntity
{
    public string Name { get; private set; } = section.GetValue<string>("name") ?? string.Empty;
    public int? Messages { get; private set; } = section.GetValue<int?>("messages");
    public int? Unacked { get; private set; } = section.GetValue<int?>("unacked");
    public string? Memory { get; private set; } = section.GetValue<string>("memory");
    public int? Consumers { get; private set; } = section.GetValue<int?>("consumers");
    public bool? CheckState { get; private set; } = section.GetValue<bool?>("check state");
    public long? MemoryNumber { get; private set; } = CommonUtil.GetSize(section.GetValue<string>("memory"), "memory");
    public string Key => Name;
    public bool IsValid => Messages.HasValue || Consumers.HasValue || CheckState.HasValue || MemoryNumber.HasValue;
    public bool Veto { get; set; }
    public string? VetoReason { get; set; }
}