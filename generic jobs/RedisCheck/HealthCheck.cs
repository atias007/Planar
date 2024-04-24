using Common;
using Microsoft.Extensions.Configuration;

namespace RedisCheck;

internal class HealthCheck : BaseDefault, ICheckElemnt
{
    private string? _usedMemory;

    public HealthCheck(IConfigurationSection section) : base(section)
    {
    }

    private HealthCheck()
    {
    }

    public bool? Ping { get; set; }
    public int? ConnectedClients { get; set; }
    public int? Latency { get; set; }

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