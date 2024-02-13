using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace Planar.Test
{
    public class Tests
    {
        public record TestRecord1(List<int> Items);
        public record TestRecord2(List<string> Items);
        public record TestRecord3(long[] Items);
        public record TestRecord4(DateTime Date);

        [Test]
        public void TestGetAll()
        {
            var list = new TestRecord1(new List<int> { 1, 2, 3, 4, 5 });
            var stringList = new TestRecord2(new List<string> { "a", "b", "c", "d", "e" });
            var longArray = new TestRecord3(new long[] { 1, 2, 3, 4, 5 });
            var entity = new TestRecord4(DateTime.Now);

            AddEntityToQueryParameter(entity);

            AddEntityToQueryParameter(list);
            AddEntityToQueryParameter(stringList);
            AddEntityToQueryParameter(longArray);
        }

        private static void AddEntityToQueryParameter<T>(T parameter)
        where T : class
        {
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var item in props)
            {
                var name = item.Name;
                var value = item.GetValue(parameter, null);
                if (value == null) { continue; }
                var type = item.PropertyType;

                if (typeof(IEnumerable).IsAssignableFrom(type))
                {
                    var arr = (IEnumerable)value;
                }

                var @default = type.IsValueType ? Activator.CreateInstance(type) : null;
                if (value.Equals(@default)) { continue; }
                var stringValue = GetStringValueForQueryStringParameter(value);
                // request.AddQueryParameter(name, stringValue, encode);
            }
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
}