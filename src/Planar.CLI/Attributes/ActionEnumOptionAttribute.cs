using System;

namespace Planar.CLI.Attributes;

[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public class ActionEnumOptionAttribute(string name) : Attribute
{
    public string Name { get; set; } = name;
}