using Planar.Common;
using System;
using System.Runtime.CompilerServices;

namespace Planar.CLI.Attributes;

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public class ActionPropertyAttribute : Attribute
{
    private readonly string decoratePropertyName;

    public ActionPropertyAttribute([CallerMemberName] string propertyName = "")
    {
        ShortName = string.Empty;
        LongName = string.Empty;
        decoratePropertyName = propertyName;
    }

    public ActionPropertyAttribute(string shortName, string longName, [CallerMemberName] string propertyName = "")
    {
        ShortName = shortName;
        LongName = longName;
        decoratePropertyName = propertyName;
    }

    public string? Name { get; set; }

    public string LongName { get; set; }

    public string ShortName { get; set; }

    public string? InputDisplay { get; set; }

    public bool Default { get; set; }

    private int _defaultOrder;

    public int DefaultOrder
    {
        get { return _defaultOrder; }
        set
        {
            _defaultOrder = value;
            Default = true;
        }
    }

    public string InputDisplayName
    {
        get
        {
            if (!string.IsNullOrEmpty(InputDisplay))
            {
                return InputDisplay;
            }

            if (!string.IsNullOrEmpty(Name))
            {
                return Name;
            }

            if (string.IsNullOrEmpty(LongName) && string.IsNullOrEmpty(ShortName))
            {
                return string.IsNullOrWhiteSpace(decoratePropertyName) ? "<no name>" : decoratePropertyName.SplitWords().ToLower();
            }

            if (string.IsNullOrEmpty(LongName))
            {
                return ShortName;
            }

            return LongName;
        }
    }

    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrEmpty(Name))
            {
                return Name;
            }

            if (string.IsNullOrEmpty(LongName) && string.IsNullOrEmpty(ShortName))
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(LongName))
            {
                return $"-{ShortName}";
            }

            if (string.IsNullOrEmpty(ShortName))
            {
                return $"--{LongName}";
            }

            return $"-{ShortName}|--{LongName}";
        }
    }
}