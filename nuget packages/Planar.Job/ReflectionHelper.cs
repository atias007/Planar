using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace Planar.Job
{
    internal static class ReflectionHelper
    {
        private static readonly ConcurrentDictionary<Type, IEnumerable<PropertyInfo>> _cache = new ConcurrentDictionary<Type, IEnumerable<PropertyInfo>>();

        public static IEnumerable<PropertyInfo> GetProperties(object instance)
        {
            return _cache.GetOrAdd(
                 instance.GetType(),
                 type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance));
        }
    }
}