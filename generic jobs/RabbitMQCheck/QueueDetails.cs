namespace RabbitMQCheck;

public class QueueDetails
{
    public string Name { get; set; } = null!;
    public int Messages { get; set; }
    public int Memory { get; set; }
    public int Consumers { get; set; }
    public string State { get; set; } = null!;

    ////[JsonProperty("messages_ready")]
    ////public int MessagesReady { get; set; }

    ////[JsonProperty("messages_dlx")]
    ////public int MessagesDlx { get; set; }

    ////[JsonProperty("messages_unacknowledged")]/
    //// /public int MessagesUnacknowledged { get; set; }

    //// public Arguments arguments { get; set; }
    //// public bool auto_delete { get; set; }
    //// public int consumer_capacity { get; set; }
    //// public int consumer_utilisation { get; set; }
    //// public bool durable { get; set; }
    //// public Effective_Policy_Definition effective_policy_definition { get; set; }
    //// public bool exclusive { get; set; }
    //// public Garbage_Collection garbage_collection { get; set; }
    //// public string leader { get; set; }
    //// public string[] members { get; set; }
    //// public int MessageBytes { get; set; }
    //// public int message_bytes_dlx { get; set; }
    //// public int message_bytes_persistent { get; set; }
    //// public int message_bytes_ram { get; set; }
    //// public int message_bytes_ready { get; set; }
    //// public int message_bytes_unacknowledged { get; set; }
    //// public int messages_persistent { get; set; }
    //// public int messages_ram { get; set; }
    //// public string node { get; set; }
    //// public string[] online { get; set; }
    //// public Open_Files open_files { get; set; }
    //// public object operator_policy { get; set; }
    //// public object policy { get; set; }
    //// public object single_active_consumer_ctag { get; set; }
    //// public string type { get; set; }
    //// public string vhost { get; set; }
}