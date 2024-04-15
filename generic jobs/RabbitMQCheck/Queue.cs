using Common;
using Microsoft.Extensions.Configuration;

namespace RabbitMQCheck;

internal class Queue(IConfigurationSection section) : BaseDefault(section), ICheckElemnt
{
    public string Name { get; private set; } = section.GetValue<string>("name") ?? string.Empty;
    public int? Messages { get; private set; } = section.GetValue<int?>("messages");
    public string? Memory { get; private set; } = section.GetValue<string>("memory");
    public int? Consumers { get; private set; } = section.GetValue<int?>("consumers");
    public bool? CheckState { get; private set; } = section.GetValue<bool?>("check state");
    public TimeSpan? Span { get; private set; } = section.GetValue<TimeSpan?>("span");

    public long? MemoryNumber { get; private set; }
    public string Key => Name;
}