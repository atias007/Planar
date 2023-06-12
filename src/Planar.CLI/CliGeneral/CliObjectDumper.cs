﻿using Spectre.Console;
using Spectre.Console.Rendering;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;

namespace Planar.CLI.CliGeneral
{
    internal static class CliObjectDumper
    {
        public static void Dump(IAnsiConsole console, object obj)
        {
            if (obj == null) { return; }

            var type = obj.GetType();
            if (type == typeof(string))
            {
                console.WriteLine(Convert.ToString(obj) ?? string.Empty);
                return;
            }

            if (obj is IEnumerable arr)
            {
                foreach (var item in arr)
                {
                    Dump(console, item);
                }

                return;
            }

            var table = GetRenderableTable(obj);
            console.Write(table);
        }

        public static Renderable GetRenderableTable(object? obj)
        {
            if (obj == null) { return new Markup(string.Empty); }

            var type = obj.GetType();
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
            return table;
        }

        private static IRenderable GetRenderableMarkup(object? value)
        {
            if (value == null)
            {
                return new Markup("[lightskyblue1][[null]][/]");
            }

            var vt = value.GetType();

            if (typeof(IDictionary).IsAssignableFrom(vt))
            {
                var dict = (IDictionary)value;
                if (dict.Count == 0)
                {
                    return new Markup("[lightskyblue1][[empty]][/]");
                }

                var table = new Table();
                table.AddColumns("key", "value");
                foreach (var item in dict.Keys)
                {
                    var dkey = Convert.ToString(item) ?? string.Empty;
                    var dvalue = Convert.ToString(dict[item]) ?? string.Empty;
                    table.AddRow(dkey, dvalue);
                }

                return table;
            }

            if (vt != typeof(string) && typeof(IEnumerable).IsAssignableFrom(vt))
            {
                var list = (IEnumerable)value;
                var table = new Table();
                table.AddColumns("child item");

                var counter = 0;
                foreach (var item in list)
                {
                    counter++;
                    var dvalue = Convert.ToString(item) ?? string.Empty;
                    table.AddRow(dvalue);
                }

                if (counter == 0)
                {
                    table.AddRow("[lightskyblue1][[empty]][/]");
                }

                table.HideHeaders();
                return table;
            }

            var text = GetRenderableString(value);
            return new Markup(text);
        }

        private static string GetRenderableString(object value)
        {
            var simpleTypes = new Type[] { typeof(byte), typeof(byte?), typeof(int), typeof(int?), typeof(short), typeof(short?), typeof(byte), typeof(byte?), typeof(long), typeof(bool), typeof(bool?) };
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