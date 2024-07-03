using Planar.CLI.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Planar.CLI.CliGeneral;

internal static class ReflectionHelper
{
    private static readonly Dictionary<Type, IEnumerable<PropertyInfo>> _properties = new();
    private static readonly Dictionary<string, ActionPropertyAttribute> _attributes = new();

    public static IEnumerable<PropertyInfo> GetPropertiesInfo<T>() where T : class
    {
        if (_properties.TryGetValue(typeof(T), out var properties))
        {
            return properties;
        }

        var result = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        _properties.Add(typeof(T), result);
        return result;
    }

    public static PropertyInfo GetPropertyInfo<T>(string name) where T : class
    {
        var properties = GetPropertiesInfo<T>();
        return properties.First(p => p.Name == name);
    }

    public static ActionPropertyAttribute GetActionPropertyAttribute<T>(string name) where T : class
    {
        var key = $"{typeof(T).FullName}.{name}";
        if (_attributes.TryGetValue(key, out var attribute))
        {
            return attribute;
        }

        var property = GetPropertyInfo<T>(name);
        var result = property.GetCustomAttribute<ActionPropertyAttribute>()
            ?? throw new InvalidDataException($"Property {name} in {typeof(T).FullName} does not have ActionPropertyAttribute");

        _attributes.Add(key, result);
        return result;
    }
}