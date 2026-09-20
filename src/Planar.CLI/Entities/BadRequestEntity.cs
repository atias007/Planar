using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Planar.CLI;

public class BadRequestEntity
{
    public string? Type { get; set; }
    public string? Title { get; set; }
    public int Status { get; set; }
    public string? TraceId { get; set; }

    public string? Detail { get; set; }

    [JsonPropertyName("errors")]
    public List<RestBadRequestError> Errors { get; set; } = new();
}

public class RestBadRequestError
{
    [JsonPropertyName("field")]
    public string Field { get; set; } = string.Empty;

    [JsonPropertyName("detail")]
    public List<string> Detail { get; set; } = [];
}