using System;

namespace Planar.CLI.Attributes;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
public class ActionAttribute(string name) : Attribute
{
    public string Name { get; private set; } = name;
}