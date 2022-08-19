using System;
using System.Collections.Generic;
using System.Linq;

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

        public static Dictionary<string, string> Merge(this Dictionary<string, string> source, Dictionary<string, string> target)
        {
            if (target == null) return source;

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

        public static string SafeTrim(this string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Trim();
        }

        public static bool HasValue(this string value)
        {
            return !string.IsNullOrEmpty(value);
        }
    }
}