using System.Collections.Generic;
using System.IO;

namespace NetEscapades.Configuration.Yaml
{
    public sealed partial class YamlConfigurationFileParser
    {
        public IDictionary<string, string?> Parse(string yml)
        {
            if (string.IsNullOrWhiteSpace(yml)) { return new Dictionary<string, string?>(); }
            using var stream = new MemoryStream();
            using var writer = new StreamWriter(stream);
            writer.Write(yml.Trim());
            writer.Flush();
            stream.Position = 0;

            var items = Parse(stream);
            return items;
        }
    }
}