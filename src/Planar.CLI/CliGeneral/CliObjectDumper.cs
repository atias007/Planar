using Spectre.Console;
using Spectre.Console.Rendering;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Planar.CLI.CliGeneral
{
    internal static class CliObjectDumper
    {
        public static void Dump(object obj)
        {
            if (obj == null) { return; }

            var type = obj.GetType();
            if (type == typeof(string))
            {
                AnsiConsole.WriteLine(Convert.ToString(obj) ?? string.Empty);
                return;
            }

            if (obj is IEnumerable arr)
            {
                foreach (var item in arr)
                {
                    Dump(item);
                }

                return;
            }

            var properties = type.GetProperties().OrderBy(p => p.Name);
            var table = new Table();
            table.AddColumns("Name", "Value");
            foreach (var p in properties)
            {
                var value = p.GetValue(obj);
                var r1 = new Markup($"[grey74]{p.Name}[/]");
                var r2 = GetRenderableMarkup(value);
                table.AddRow(r1, r2);
            }

            table.HideHeaders();
            AnsiConsole.Write(table);
        }

        private static IRenderable GetRenderableMarkup(object? value)
        {
            if (value == null)
            {
                return new Markup("[lightskyblue1][[null]][/]");
            }

            var vt = value.GetType();
            var dictionaryType = new Type[] { typeof(Dictionary<string, string>), typeof(SortedDictionary<string, string>) };

            if (dictionaryType.Contains(vt))
            {
                var dict = (IDictionary<string, string>)value;
                if (dict.Count == 0)
                {
                    return new Markup("[lightskyblue1][[empty]][/]");
                }

                var table = new Table();
                table.AddColumns("key", "value");
                foreach (var item in dict)
                {
                    table.AddRow(item.Key, item.Value);
                }

                return table;
            }

            var text = GetRenderable(value);
            return new Markup(text);
        }

        private static string GetRenderable(object value)
        {
            var simpleTypes = new Type[] { typeof(byte), typeof(byte?), typeof(int), typeof(int?), typeof(long), typeof(bool), typeof(bool?) };
            var dateTypes = new Type[] { typeof(DateTime), typeof(DateTime?) };
            var timeSpanTypes = new Type[] { typeof(TimeSpan), typeof(TimeSpan?) };

            var vt = value.GetType();
            if (vt == typeof(string)) { return $"{value.ToString().EscapeMarkup()}"; }
            if (simpleTypes.Contains(vt)) { return $"{value}"; }
            if (dateTypes.Contains(vt))
            {
                var dtValue = Convert.ToDateTime(value);
                return $"{CliTableFormat.FormatDateTime(dtValue)}";
            }

            if (timeSpanTypes.Contains(vt))
            {
                var tsValue = (TimeSpan)value;
                return $"{CliTableFormat.FormatTimeSpan(tsValue)}";
            }

            return "[red]not supported[/]";
        }
    }
}