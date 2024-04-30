using System;

namespace Planar.CLI.Attributes;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
internal class IgnoreHelpAttribute : Attribute
{
}