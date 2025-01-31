using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Planar.Client.Entities
{
    public class PlanarError
    {
#if NETSTANDARD2_0

        [JsonPropertyName("field")]
        public string Field { get; set; }

        [JsonPropertyName("detail")]
        public List<string> Detail { get; set; }

#else
        [JsonPropertyName("field")]
        public string? Field { get; set; }

        [JsonPropertyName("detail")]
        public List<string>? Detail { get; set; }
#endif
    }
}