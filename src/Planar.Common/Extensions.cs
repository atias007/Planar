using Planar.API.Common.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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

        public static bool Is<T>(this Type type) where T : struct
        {
            return type == typeof(T) || type == typeof(Nullable<T>);
        }
    }
}