using System;
using System.Globalization;
using System.Reflection;

namespace RestSharp;

public static class MyRestRequestExtensions
{
    public static RestRequest AddEntityToQueryParameter<T>(this RestRequest request, T parameter, bool encode = true)
        where T : class
    {
        var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var item in props)
        {
            var name = item.Name;

            // TODO: add support for ienumerable
            var value = item.GetValue(parameter, null);
            if (value == null) { continue; }

            var type = value.GetType();
            var @default = type.IsValueType ? Activator.CreateInstance(type) : null;
            if (value.Equals(@default)) { continue; }
            var stringValue = GetStringValueForQueryStringParameter(value);
            request.AddQueryParameter(name, stringValue, encode);
        }

        return request;
    }

    private static string? GetStringValueForQueryStringParameter(object value)
    {
        const string DateFormat = "s";

        if (value is DateTime)
        {
            var dateValue = (DateTime)value;
            return dateValue.ToString(DateFormat);
        }

        if (value is DateTimeOffset)
        {
            var dateValue = (DateTimeOffset)value;
            return dateValue.ToString(DateFormat);
        }

        return Convert.ToString(value, CultureInfo.CurrentCulture);
    }
}