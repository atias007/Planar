using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliUpdateJobRequest : CliAddJobRequest
    {
        [ActionProperty(DefaultOrder = 1)]
        public string Options { get; set; }
    }
}