using System;

namespace Planar.CLI.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class ModuleAttribute(string name, string description) : Attribute
{
    public string Name { get; private set; } = name;

    public string Synonyms { get; set; } = string.Empty;

    public string Description { get; private set; } = description;
}