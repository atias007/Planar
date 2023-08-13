using Planar.Job;
using System.Collections.Generic;

namespace Planar.Common
{
    internal static class DataMapUtils
    {
        public static DataMap Convert(Dictionary<string, object?> source)
        {
            var result = new DataMap();
            if (source == null) { return result; }

            foreach (var item in source)
            {
                var strValue = PlanarConvert.ToString(item.Value);
                result.TryAdd(item.Key, strValue);
            }

            return result;
        }
    }
}