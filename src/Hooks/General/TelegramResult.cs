using System.Text.Json.Serialization;

namespace Planar.Hooks.General;

public class TelegramResult
{
    public bool Ok { get; set; }
    public Result? Result { get; set; }
}

public class Result
{
    [JsonPropertyName("message_id")]
    public int MessageId { get; set; }
}