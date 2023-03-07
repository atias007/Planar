using System;
using System.Collections.Generic;

namespace Planar.CLI.CliGeneral
{
    public class CliModule
    {
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public IEnumerable<CliActionMetadata> Actions { get; set; } = new List<CliActionMetadata>();
    }
}