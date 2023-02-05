using System.Collections.Generic;

namespace Planar.CLI.Entities
{
    internal class CliGeneralMarupMessageResponse
    {
        public string Title { get; set; } = string.Empty;

        public List<string> MarkupMessages { get; set; } = new();
    }
}