using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Planar.CLI
{
    internal static class Util
    {
        public static string LastJobOrTriggerId { get; set; }

        public static string BeautifyJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            var result = JToken.Parse(json).ToString(Formatting.Indented);
            return result;
        }
    }
}