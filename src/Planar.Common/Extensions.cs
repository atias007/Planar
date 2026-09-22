using Planar.API.Common.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Planar.Common
{
    public static class Extensions
    {
        public static bool IsNullOrEmpty<T>(this List<T> list)
        {
            return list == null || list.Count == 0;
        }

        public static bool NotContains<T>(this IEnumerable<T> list, T value)
        {
            return list == null || !list.Contains(value);
        }

        public static IEnumerable<TSource> SetPaging<TSource>(this IEnumerable<TSource> source, IPagingRequest pagingRequest)
        {
            pagingRequest.SetPagingDefaults();
            var page = pagingRequest.PageNumber.GetValueOrDefault();
            var size = pagingRequest.PageSize.GetValueOrDefault();
            return source
                .Skip((page - 1) * size)
                .Take(size);
        }

        public static void Put<TValue>(this Dictionary<string, TValue?> dictionary, string key, TValue? value)
        {
            ArgumentNullException.ThrowIfNull(dictionary);
            if (!dictionary.TryAdd(key, value))
            {
                dictionary[key] = value;
            }
        }

        public static void Put(this Dictionary<string, string?> dictionary, string key, string? value)
        {
            Put<string>(dictionary, key, value);
        }

        public static Dictionary<string, string?> Merge(this Dictionary<string, string?> source, IDictionary<string, string?> target)
        {
            if (target == null) { return source; }
            source ??= [];

            foreach (var item in target)
            {
                if (source.ContainsKey(item.Key))
                {
                    source[item.Key] = item.Value;
                }
                else
                {
                    source.Add(item.Key, item.Value);
                }
            }

            return source;
        }

        public static string ToSimpleTimeString(this TimeSpan span)
        {
            return span.ToString(@"hh\:mm\:ss");
        }

        public static string? SafeTrim(this string? value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Trim();
        }

        public static bool HasValue([NotNullWhen(true)] this string? value)
        {
            return !string.IsNullOrEmpty(value);
        }

        public static string SplitWords(this string value)
        {
            const string spacer = " ";
            const string template = @"(?<=[A-Z])(?=[A-Z][a-z])|(?<=[^A-Z])(?=[A-Z])|(?<=[A-Za-z])(?=[^A-Za-z])";
            var r = new Regex(template, RegexOptions.None, TimeSpan.FromMilliseconds(500));
            var result = r.Replace(value, spacer);
            return result;
        }

        public static string ToKebabCase(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var sb = new StringBuilder(input.Length + 8);

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];

                if (char.IsUpper(c))
                {
                    if (i > 0)
                    {
                        char prev = input[i - 1];
                        bool nextIsLower = i + 1 < input.Length && char.IsLower(input[i + 1]);

                        // Word boundary: "myWord" / "v2Beta", or end of an acronym: "XMLParser"
                        if (char.IsLower(prev) || char.IsDigit(prev) || (char.IsUpper(prev) && nextIsLower))
                            sb.Append('-');
                    }
                    sb.Append(char.ToLowerInvariant(c));
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        public static bool Is<T>(this Type type) where T : struct
        {
            return type == typeof(T) || type == typeof(Nullable<T>);
        }
    }
}