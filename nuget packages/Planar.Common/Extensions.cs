using Planar.Job;
using System.Collections.Generic;

namespace Planar.Common
{
    internal static class Extensions
    {
        public static void AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (dictionary.ContainsKey(key))
            {
                dictionary[key] = value;
            }
            else
            {
                dictionary.Add(key, value);
            }
        }

        public static DataMap Merge(this DataMap dataMap, DataMap source)
        {
            var merge = MergeInner(dataMap, source);
            var result = new DataMap(merge);
            return result;
        }

#if NETSTANDARD2_0

        public static Dictionary<string, string> Merge(this Dictionary<string, string> dictionary, Dictionary<string, string> source)
#else
        public static Dictionary<string, string?> Merge(this Dictionary<string, string?> dictionary, Dictionary<string, string?> source)
#endif
        {
            var merge = MergeInner(dictionary, source);
#if NETSTANDARD2_0
            var result = new Dictionary<string, string>(merge);
#else
            var result = new Dictionary<string, string?>(merge);
#endif

            return result;
        }

#if NETSTANDARD2_0

        public static Dictionary<string, string> Merge(this Dictionary<string, string> dictionary, IDictionary<string, string> source)
#else
        public static Dictionary<string, string?> Merge(this Dictionary<string, string?> dictionary, IDictionary<string, string?> source)
#endif
        {
            var merge = MergeInner(dictionary, source);
#if NETSTANDARD2_0
            var result = new Dictionary<string, string>(merge);
#else
            var result = new Dictionary<string, string?>(merge);
#endif
            return result;
        }

#if NETSTANDARD2_0

        public static SortedDictionary<string, string> Merge(this SortedDictionary<string, string> dictionary, SortedDictionary<string, string> source)
#else
        public static SortedDictionary<string, string?> Merge(this SortedDictionary<string, string?> dictionary, SortedDictionary<string, string?> source)
#endif

        {
            var merge = MergeInner(dictionary, source);
#if NETSTANDARD2_0
            var result = new SortedDictionary<string, string>(merge);
#else
            var result = new SortedDictionary<string, string?>(merge);
#endif
            return result;
        }

#if NETSTANDARD2_0

        private static IDictionary<string, string> MergeInner(IDictionary<string, string> dictionary, IDictionary<string, string> source)
#else
        private static IDictionary<string, string?> MergeInner(IDictionary<string, string?> dictionary, IDictionary<string, string?> source)
#endif

        {
            if (source == null) { return dictionary; }

#if NETSTANDARD2_0
            if (dictionary == null) { dictionary = new SortedDictionary<string, string>(); }
#else
            dictionary ??= new SortedDictionary<string, string?>();
#endif

            foreach (var item in source)
            {
                if (dictionary.ContainsKey(item.Key))
                {
                    dictionary[item.Key] = item.Value;
                }
                else
                {
                    dictionary.Add(item.Key, item.Value);
                }
            }

            return dictionary;
        }
    }
}