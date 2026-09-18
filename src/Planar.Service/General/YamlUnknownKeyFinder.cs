using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace YamlExtras;

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
    private readonly ITypeInspector _inspector;
    private readonly StringComparer _comparer;
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

    private Dictionary<string, Type> MembersOf(Type type) => _members.GetOrAdd(type, t =>
    {
        var map = new Dictionary<string, Type>(_comparer);
        foreach (var property in _inspector.GetProperties(t, null))
        {
            map[property.Name] = property.Type;
        }
        return map;
    });

    private static string Append(string path, string key)
        => key.Length == 0 || key.Contains('.') || key.Contains(' ') ? $"{path}[\"{key}\"]" : $"{path}.{key}";

    private static string KeyText(YamlNode key)
        => key is YamlScalarNode scalar ? scalar.Value ?? string.Empty : key.ToString();

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

    private static Type? ClosedGeneric(Type type, Type openGeneric)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == openGeneric)
        {
            return type;
        }
        return type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == openGeneric);
    }
}