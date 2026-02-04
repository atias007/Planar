using Planar.API.Common;
using Planar.Common;
using Spectre.Console;
using Spectre.Console.Rendering;
using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Planar.CLI.CliGeneral;

internal static class CliObjectDumper
{
    public static void Dump(IAnsiConsole console, CliDumpObject dumpObject)
    {
        var obj = dumpObject.Object;
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
                var dumpItem = new CliDumpObject(item);
                Dump(console, dumpItem);
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
            var r1 = GetRenderableMarkupName(p);
            if (r1 == null) { continue; }
            var r2 = GetRenderableMarkupValue(value, p);
            table.AddRow(r1, r2);
        }

        table.HideHeaders();
        return table;
    }

    private static Markup? GetRenderableMarkupName(PropertyInfo propertyInfo)
    {
        var attr = propertyInfo.GetCustomAttribute<DisplayFormatAttribute>();
        if (attr?.Hide ?? false) { return null; }

        var display = string.IsNullOrWhiteSpace(attr?.DisplayName) ? propertyInfo.Name.SplitWords() : attr.DisplayName;
        return new Markup($"[grey74]{display}[/]");
    }

    private static IRenderable GetRenderableMarkupValue(object? value, PropertyInfo propertyInfo)
    {
        if (value == null)
        {
            return new Markup("[lightskyblue1][[null]][/]");
        }

        var vt = propertyInfo.PropertyType;

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

        var text = GetRenderableString(value, propertyInfo);
        return new Markup(text);
    }

    private static string GetRenderableString(object value, PropertyInfo propertyInfo)
    {
        var simpleTypes = new Type[] { typeof(byte), typeof(byte?), typeof(int), typeof(int?), typeof(short), typeof(short?), typeof(byte), typeof(byte?), typeof(long), typeof(bool), typeof(bool?) };
        var dateTypes = new Type[] { typeof(DateTime), typeof(DateTime?) };
        var timeSpanTypes = new Type[] { typeof(TimeSpan), typeof(TimeSpan?) };

        var vt = propertyInfo.PropertyType;
        var attr = propertyInfo.GetCustomAttribute<DisplayFormatAttribute>();
        var specialFormat = attr == null ? SpecialFormat.None : attr.SpecialFormat;
        if (vt == typeof(string))
        {
            var text = value.ToString();
            if (attr?.MaximumChars > 0 && text?.Length > attr?.MaximumChars)
            {
                text = string.Concat(text.AsSpan(0, attr.MaximumChars), $"\r\n…\r\n(this property display only top {attr?.MaximumChars:N0} charecters)");
            }

            if (specialFormat == SpecialFormat.Log)
            {
                return CliFormat.GetLogMarkup(text) ?? string.Empty;
            }

            return $"{text.EscapeMarkup()}";
        }

        // check for DisplayFormatAttribute
        if (attr != null && value is IFormattable formattable)
        {
            if (specialFormat == SpecialFormat.Duration)
            {
                return $"{CliTableFormat.FromatDuration((int)value)}";
            }

            return formattable.ToString(attr.Format, CultureInfo.CurrentCulture).EscapeMarkup();
        }

        if (simpleTypes.Contains(vt))
        {
            return $"{value}";
        }

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