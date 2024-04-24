using Common;
using Microsoft.Extensions.Configuration;

namespace RabbitMQCheck;

internal class Queue(IConfigurationSection section) : BaseDefault(section), INamedCheckElement
{
    public string Name { get; private set; } = section.GetValue<string>("name") ?? string.Empty;
    public int? Messages { get; private set; } = section.GetValue<int?>("messages");
    public string? Memory { get; private set; } = section.GetValue<string>("memory");
    public int? Consumers { get; private set; } = section.GetValue<int?>("consumers");
    public bool? CheckState { get; private set; } = section.GetValue<bool?>("check state");
    public long? MemoryNumber { get; private set; }
    public string Key => Name;
    public bool Active { get; private set; } = section.GetValue<bool?>("active") ?? true;
    public bool IsValid => Messages.HasValue || Consumers.HasValue || CheckState.HasValue || MemoryNumber.HasValue;

    public void SetSize()
    {
        MemoryNumber = CommonUtil.GetSize(Memory, "memory");
    }
}