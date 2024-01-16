using System;

namespace Planar.CLI.CliGeneral
{
    [AttributeUsage(AttributeTargets.Property)]
    internal class CliFormatAttribute : Attribute
    {
        public string Format { get; set; } = string.Empty;
    }
}
