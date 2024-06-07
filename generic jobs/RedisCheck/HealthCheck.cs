using Common;
using Microsoft.Extensions.Configuration;

namespace RedisCheck;

internal class HealthCheck : BaseDefault, ICheckElement
{
    private string? _usedMemory;

    public HealthCheck(IConfigurationSection section) : base(section)
    {
        Ping = section.GetValue<bool?>("ping");
        ConnectedClients = section.GetValue<int?>("connected clients");
        Latency = section.GetValue<int?>("latency");
        UsedMemory = section.GetValue<string>("used memory");
        Active = section.GetValue<bool?>("active") ?? true;
    }

    private HealthCheck()
    {
    }

    public bool? Ping { get; private set; }
    public int? ConnectedClients { get; private set; }
    public int? Latency { get; private set; }
    public bool Active { get; private set; } = true;

    public string? UsedMemory
    {
        get { return _usedMemory; }
        set
        {
            _usedMemory = value;
            UsedMemoryNumber = CommonUtil.GetSize(_usedMemory, "used memory");
        }
    }

    public long? UsedMemoryNumber { get; private set; }

    public string Key => "health check";

    public static HealthCheck Empty => new();
}