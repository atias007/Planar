using System.Collections.Generic;
using System.IO;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Planar.Common;

public static class YmlUtil
{
    public static T Deserialize<T>(string yml)
        where T : class
    {
        return YmlDeserializer.Deserialize<T>(yml);
    }

    public static string Serialize<T>(T item)
        where T : class
    {
        if (item == null) { return string.Empty; }
        return YmlSerializer.Serialize(item);
    }

    public static IEnumerable<KeyValuePair<string, string>> SplitByKind(string yamlText)
    {
        yamlText = yamlText.ReplaceLineEndings("\n");   // keeps line numbers and slices aligned

        // 1. where does each document begin? (Mark.Line is 1-based)
        var startLines = new List<int>();
        var parser = new Parser(new StringReader(yamlText));
        while (parser.MoveNext())
            if (parser.Current is DocumentStart ds)
                startLines.Add((int)ds.Start.Line);

        // 2. slice the original text on those boundaries
        var lines = yamlText.Split('\n');
        var deserializer = new DeserializerBuilder().Build();
        var result = new List<KeyValuePair<string, string>>();

        for (int i = 0; i < startLines.Count; i++)
        {
            int from = startLines[i] - 1;
            int to = i + 1 < startLines.Count ? startLines[i + 1] - 1 : lines.Length;

            var raw = string.Join("\n", lines[from..to]).Trim();
            if (raw.Length == 0) continue;                       // empty doc between two ---

            var kind = string.Empty;
            var doc = deserializer.Deserialize<Dictionary<string, object>>(new StringReader(raw));
            if (doc is not null && doc.TryGetValue("kind", out var v) && v is string s)
                kind = s;

            result.Add(new KeyValuePair<string, string>(kind, raw));
        }

        return result;
    }

    private static IDeserializer YmlDeserializer
    {
        get
        {
            var deserializer = new DeserializerBuilder()
                        .WithNamingConvention(CamelCaseNamingConvention.Instance)
                        .IgnoreUnmatchedProperties()
                        .Build();
            return deserializer;
        }
    }

    private static ISerializer YmlSerializer
    {
        get
        {
            var serializer = new SerializerBuilder()
                        .WithNamingConvention(CamelCaseNamingConvention.Instance)
                        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
                        .Build();
            return serializer;
        }
    }
}