using System;
using System.Collections.Generic;
using System.Reflection;

namespace Planar.Job
{
    internal static class ReflectionHelper
    {
        private static readonly Dictionary<Type, IEnumerable<PropertyInfo>> _cache = new Dictionary<Type, IEnumerable<PropertyInfo>>();
        private static readonly object _lock = new object();

        public static IEnumerable<PropertyInfo> GetProperties(object instance)
        {
            var type = instance.GetType();
            if (_cache.TryGetValue(type, out var result)) { return result; }
            lock (_lock)
            {
                if (_cache.TryGetValue(type, out result)) { return result; }
                result = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                _cache[type] = result;
                return result;
            }
        }
    }
}