using Newtonsoft.Json;

namespace RabbitMQCheck;

internal class NodeDetails
{
    [JsonProperty("disk_free")]
    public long DiskFree { get; set; }

    [JsonProperty("disk_free_alarm")]
    public bool DiskFreeAlarm { get; set; }

    [JsonProperty("disk_free_limit")]
    public long DiskFreeLimit { get; set; }

    [JsonProperty("mem_alarm")]
    public bool MemoryAlarm { get; set; }

    [JsonProperty("mem_limit")]
    public long MemoryLimit { get; set; }

    [JsonProperty("mem_used")]
    public long MemoryUsed { get; set; }

    public string Name { get; set; } = null!;

    public bool Running { get; set; }
}