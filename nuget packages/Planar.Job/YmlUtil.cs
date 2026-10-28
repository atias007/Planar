using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Planar.Common
{
    internal static class YmlUtil
    {
        private readonly static DeserializerBuilder YmlReserializeBuilder =
            new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties();

        private readonly static ISerializer YmlSerializer =
            new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
                .Build();

        private readonly static IDeserializer YmlReserialize = YmlReserializeBuilder.Build();

        public static string AddSourceFilenameToManifestYml(string ymlContent, string sourceFilename)
        {
            const string separator = "---";
            const string source = "source: ";

            var items = SplitByKind(ymlContent);
            var final = new StringBuilder();
            foreach (var item in items)
            {
                var content = item.Value.Trim();
                if (string.IsNullOrWhiteSpace(content)) { continue; }
                if (content == separator) { continue; }
                if (content.StartsWith(separator)) { content = content.Substring(separator.Length); }
                if (content.EndsWith(separator)) { content = content.Substring(0, content.Length - separator.Length); }
                final.AppendLine($"{source}{sourceFilename}");
                final.AppendLine(content.Trim());
                final.AppendLine(separator);
            }

            var result = final.ToString().Trim();
            if (result.EndsWith(separator))
            {
                result = result.Substring(0, result.Length - separator.Length);
            }

            return result;
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

                var arr = new string[to - from];
                Array.Copy(lines, from, arr, 0, to - from);

                var raw = string.Join("\n", arr).Trim();
                if (raw.Length == 0) continue;                       // empty doc between two ---

                var kind = string.Empty;
                var doc = YmlReserialize.Deserialize<Dictionary<string, object>>(new StringReader(raw));
                if (!(doc is null) && doc.TryGetValue("kind", out var v) && v is string s)
                    kind = s;

                result.Add(new KeyValuePair<string, string>(kind, raw));
            }

            return result;
        }
    }

    public static class StringExtensions
    {
        public static string ReplaceLineEndings(this string text)
        {
            return ReplaceLineEndings(text, Environment.NewLine);
        }

        public static string ReplaceLineEndings(this string text, string replacementText)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            if (replacementText == null) throw new ArgumentNullException(nameof(replacementText));

            int first = IndexOfNewLineChar(text, 0);
            if (first < 0)
                return text; // no line endings, so return the original string with no allocation

            var sb = new StringBuilder(text.Length + 16);
            int start = 0;
            int i = first;

            while (i >= 0)
            {
                sb.Append(text, start, i - start);
                sb.Append(replacementText);

                // Treat CRLF as a single line ending
                if (text[i] == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                    i++;

                start = i + 1;
                i = IndexOfNewLineChar(text, start);
            }

            sb.Append(text, start, text.Length - start);
            return sb.ToString();
        }

        private static readonly char[] NewLineChars =
        {
        '\r', '\n', '\f', '\u0085', '\u2028', '\u2029'
    };

        private static int IndexOfNewLineChar(string text, int startIndex)
        {
            return startIndex < text.Length ? text.IndexOfAny(NewLineChars, startIndex) : -1;
        }
    }
}