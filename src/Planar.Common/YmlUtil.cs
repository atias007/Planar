using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Planar.Common;

public static class YmlUtil
{
    private readonly static DeserializerBuilder YmlDeserializerBuilder =
        new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties();

    private readonly static ISerializer YmlSerializer =
        new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build();

    private readonly static IDeserializer YmlDeserializer = YmlDeserializerBuilder.Build();

    public static T Deserialize<T>(string yml)
                    where T : class
    {
        return YmlDeserializer.Deserialize<T>(yml);
    }

    public static string GetApplySource(string yml)
    {
        if (string.IsNullOrWhiteSpace(yml)) { return string.Empty; }
        try
        {
            const string source = "source:";
            var lines = yml.Split('\n');
            foreach (var item in lines)
            {
                var index = item.IndexOf(source, StringComparison.OrdinalIgnoreCase);
                if (index < 0) { continue; }
                if (item.Length == source.Length) { return string.Empty; }
                return item[(index + source.Length)..].Trim();
            }
        }
        catch
        {
            // DO NOTHING //
        }

        return string.Empty;
    }

    public static string GetUnmatchedMessage<T>(string yaml)
    {
        var unmatched = YmlUtil.FindUnmatched<T>(yaml);
        if (unmatched.Count == 0) { return string.Empty; }
        var source = YmlUtil.GetApplySource(yaml);
        string message;
        if (string.IsNullOrWhiteSpace(source))
        {
            message = $"the following properties has no match:\r\n{string.Join("\r\n", unmatched)}";
        }
        else
        {
            message = $"the following properties, in {source} file, has no match:\r\n{string.Join("\r\n", unmatched)}";
        }

        return message;
    }

    public static string Serialize<T>(T item)
        where T : class
    {
        if (item == null) { return string.Empty; }
        return YmlSerializer.Serialize(item);
    }

    public static List<KeyValuePair<string, string>> SplitByKind(string yamlText)
    {
        try
        {
            return SplitByKindInner(yamlText);
        }
        catch (Exception ex)
        {
            throw new InvalidCastException($"failed read yaml file. check file content to be valid yaml format. message: {ex.Message}");
        }
    }

    private static List<KeyValuePair<string, string>> SplitByKindInner(string yamlText)
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
        var result = new List<KeyValuePair<string, string>>();

        for (int i = 0; i < startLines.Count; i++)
        {
            int from = startLines[i] - 1;
            int to = i + 1 < startLines.Count ? startLines[i + 1] - 1 : lines.Length;

            var raw = string.Join("\n", lines[from..to]).Trim();
            if (raw.Length == 0) continue;                       // empty doc between two ---

            var kind = string.Empty;
            var doc = YmlDeserializer.Deserialize<Dictionary<string, object>>(new StringReader(raw));
            if (doc is not null && doc.TryGetValue("kind", out var v) && v is string s)
                kind = s;

            result.Add(new KeyValuePair<string, string>(kind, raw));
        }

        return result;
    }

    private static IReadOnlyList<UnknownYamlKey> FindUnmatched<T>(string yml)
    {
        var finder = YamlUnknownKeyFinder.From(YmlDeserializerBuilder);
        var unknown = finder.Find<T>(yml);
        return unknown;
    }
}

/// <summary>
/// A key present in the YAML document that has no matching member on the target type.
/// </summary>
/// <param name="Path">Dotted path to the key, e.g. <c>$.server.tls.minVersoin</c> or <c>$.routes[2].clusterr</c>.</param>
/// <param name="Key">The key itself, as written in the YAML.</param>
/// <param name="Value">The value node, so you can read it, re-serialize it, or deserialize it into something else.</param>
/// <param name="Line">1-based line of the key in the source document.</param>
/// <param name="Column">1-based column of the key in the source document.</param>
public readonly record struct UnknownYamlKey(string Path, string Key, YamlNode Value, int Line, int Column)
{
    public override string ToString() => $"{Path} (line {Line}, col {Column})";
}

/// <summary>
/// Finds keys in a YAML document that do not map to any member of the target type.
/// <para>
/// It reuses the deserializer's own <see cref="ITypeInspector"/>, so the naming convention,
/// <c>[YamlMember(Alias = ...)]</c> and <c>[YamlIgnore]</c> are honored exactly as during deserialization.
/// </para>
/// </summary>
/// <remarks>
/// Thread-safe and reusable: build one per deserializer configuration and keep it.
/// Note this parses the document a second time — cheap for config files, measure before using it on hot paths.
/// </remarks>
public sealed class YamlUnknownKeyFinder
{
    private readonly StringComparer _comparer;
    private readonly ITypeInspector _inspector;
    private readonly ConcurrentDictionary<Type, Dictionary<string, Type>> _members = new();

    public YamlUnknownKeyFinder(ITypeInspector inspector, bool caseInsensitive = false)
    {
        _inspector = inspector ?? throw new ArgumentNullException(nameof(inspector));
        _comparer = caseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
    }

    /// <summary>
    /// Builds a finder from the same builder used to create the deserializer, so both agree on
    /// naming convention and member visibility. Pass the builder *after* all With... calls.
    /// </summary>
    public static YamlUnknownKeyFinder From(DeserializerBuilder builder, bool caseInsensitive = false)
        => new((builder ?? throw new ArgumentNullException(nameof(builder))).BuildTypeInspector(), caseInsensitive);

    public IReadOnlyList<UnknownYamlKey> Find<T>(string yaml) => Find(yaml, typeof(T));

    public IReadOnlyList<UnknownYamlKey> Find<T>(TextReader reader) => Find(reader, typeof(T));

    public IReadOnlyList<UnknownYamlKey> Find(string yaml, Type rootType)
    {
        using var reader = new StringReader(yaml ?? throw new ArgumentNullException(nameof(yaml)));
        return Find(reader, rootType);
    }

    public IReadOnlyList<UnknownYamlKey> Find(TextReader reader, Type rootType)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(rootType);

        var stream = new YamlStream();
        stream.Load(reader);

        var found = new List<UnknownYamlKey>();
        foreach (var document in stream.Documents)
        {
            Walk(document.RootNode, rootType, "$", found, new HashSet<YamlNode>());
        }
        return found;
    }

    private static string Append(string path, string key)
        => key.Length == 0 || key.Contains('.') || key.Contains(' ') ? $"{path}[\"{key}\"]" : $"{path}.{key}";

    private static Type? ClosedGeneric(Type type, Type openGeneric)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == openGeneric)
        {
            return type;
        }
        return type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == openGeneric);
    }

    private static Type? DictionaryValueType(Type type)
    {
        var generic = ClosedGeneric(type, typeof(IDictionary<,>)) ?? ClosedGeneric(type, typeof(IReadOnlyDictionary<,>));
        if (generic is not null)
        {
            return generic.GetGenericArguments()[1];
        }
        return typeof(IDictionary).IsAssignableFrom(type) ? typeof(object) : null;
    }

    private static Type? EnumerableElementType(Type type)
    {
        if (type.IsArray)
        {
            return type.GetElementType();
        }
        var generic = ClosedGeneric(type, typeof(IEnumerable<>));
        if (generic is not null)
        {
            return generic.GetGenericArguments()[0];
        }
        return typeof(IEnumerable).IsAssignableFrom(type) ? typeof(object) : null;
    }

    private static bool IsLeaf(Type type)
        => type.IsPrimitive
        || type.IsEnum
        || type == typeof(string)
        || type == typeof(decimal)
        || type == typeof(DateTime)
        || type == typeof(DateTimeOffset)
        || type == typeof(TimeSpan)
        || type == typeof(Guid)
        || type == typeof(Uri);

    private static string KeyText(YamlNode key)
        => key is YamlScalarNode scalar ? scalar.Value ?? string.Empty : key.ToString();

    private Dictionary<string, Type> MembersOf(Type type) => _members.GetOrAdd(type, t =>
    {
        var map = new Dictionary<string, Type>(_comparer);
        foreach (var property in _inspector.GetProperties(t, null))
        {
            map[property.Name] = property.Type;
        }
        return map;
    });

    private void Walk(YamlNode node, Type type, string path, List<UnknownYamlKey> found, HashSet<YamlNode> visited)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        // Nothing to match against: the target accepts anything the YAML happens to contain.
        if (type == typeof(object) || typeof(YamlNode).IsAssignableFrom(type) || IsLeaf(type))
        {
            return;
        }

        // Aliases (*anchor) resolve to the same node instance; walk each node once to avoid cycles.
        if (!visited.Add(node))
        {
            return;
        }

        switch (node)
        {
            case YamlMappingNode map:
                WalkMapping(map, type, path, found, visited);
                break;

            case YamlSequenceNode sequence:
                var elementType = EnumerableElementType(type);
                if (elementType is null)
                {
                    break;
                }
                for (var i = 0; i < sequence.Children.Count; i++)
                {
                    Walk(sequence.Children[i], elementType, $"{path}[{i}]", found, visited);
                }
                break;
        }

        visited.Remove(node);
    }

    private void WalkMapping(YamlMappingNode map, Type type, string path, List<UnknownYamlKey> found, HashSet<YamlNode> visited)
    {
        // A dictionary target takes arbitrary keys — none of them are "unknown",
        // but their values may still contain unknown keys deeper down.
        var dictionaryValueType = DictionaryValueType(type);
        if (dictionaryValueType is not null)
        {
            foreach (var entry in map.Children)
            {
                Walk(entry.Value, dictionaryValueType, Append(path, KeyText(entry.Key)), found, visited);
            }
            return;
        }

        var members = MembersOf(type);
        foreach (var entry in map.Children)
        {
            var key = KeyText(entry.Key);
            var childPath = Append(path, key);

            if (members.TryGetValue(key, out var memberType))
            {
                Walk(entry.Value, memberType, childPath, found, visited);
            }
            else
            {
                var mark = entry.Key.Start;
                found.Add(new UnknownYamlKey(childPath, key, entry.Value, (int)mark.Line, (int)mark.Column));
            }
        }
    }
}