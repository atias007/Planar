using System.Collections.Generic;
using System.IO;

namespace NetEscapades.Configuration.Yaml
{
    internal sealed partial class YamlConfigurationFileParser
    {
#if NETSTANDARD2_0

        public IDictionary<string, string> Parse(string yml)
        {
            if (string.IsNullOrWhiteSpace(yml)) { return new Dictionary<string, string>(); }
            using (var stream = new MemoryStream())
            {
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(yml.Trim());
                    writer.Flush();
                    stream.Position = 0;

                    var items = Parse(stream);
                    return items;
                }
            }
        }

#else
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

#endif
    }
}