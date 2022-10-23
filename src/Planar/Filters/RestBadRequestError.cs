using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Planar.Filters
{
    internal class RestBadRequestError
    {
        [JsonPropertyName("field")]
        public string Field { get; set; }

        [JsonPropertyName("detail")]
        public List<string> Detail { get; set; }
    }
}