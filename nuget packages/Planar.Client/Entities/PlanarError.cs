using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Planar.Client.Entities
{
    public class PlanarError
    {
        [JsonPropertyName("field")]
        public string? Field { get; set; }

        [JsonPropertyName("detail")]
        public List<string>? Detail { get; set; }
    }
}