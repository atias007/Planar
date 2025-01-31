using Planar.Job;
using System.Collections.Generic;

namespace Planar.Common
{
    internal static class DataMapUtils
    {
#if NETSTANDARD2_0

        public static DataMap Convert(IDictionary<string, object> source)
#else
        public static DataMap Convert(IDictionary<string, object?> source)
#endif
        {
            var result = new DataMap();
            if (source == null) { return result; }

            foreach (var item in source)
            {
                var strValue = PlanarConvert.ToString(item.Value);

#if NETSTANDARD2_0
                if (!result.ContainsKey(item.Key))
                {
                    result.Add(item.Key, strValue);
                }
#else
                result.TryAdd(item.Key, strValue);
#endif
            }

            return result;
        }
    }
}