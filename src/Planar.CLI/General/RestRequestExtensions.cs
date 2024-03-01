using System;
using System.Collections;
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
            var value = item.GetValue(parameter, null);
            if (value == null) { continue; }
            var type = item.PropertyType;

            if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string))
            {
                var arr = (IEnumerable)value;
                foreach (var arrItem in arr)
                {
                    AddQueryParameter(name, arrItem, arrItem.GetType(), encode);
                }
            }
            else
            {
                AddQueryParameter(name, value, type, encode);
            }

            void AddQueryParameter(string name, object value, Type type, bool encode)
            {
                var @default = type.IsValueType ? Activator.CreateInstance(type) : null;
                if (value.Equals(@default)) { return; }
                var stringValue = GetStringValueForQueryStringParameter(value);
                request.AddQueryParameter(name, stringValue, encode);
            }
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