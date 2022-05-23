using Newtonsoft.Json;
using System;

namespace Planar
{
    public static class JsonMapper
    {
        public static TTarget Map<TTarget, TSource>(TSource source)
            where TTarget : class, new()
            where TSource : class, new()
        {
            if (source == null) { return null; }
            var json = JsonConvert.SerializeObject(source);
            var target = JsonConvert.DeserializeObject<TTarget>(json);
            return target;
        }

        public static object Map(object source, Type targetType)
        {
            if (source == null) { return null; }
            var json = JsonConvert.SerializeObject(source);
            var target = JsonConvert.DeserializeObject(json, targetType);
            return target;
        }
    }
}