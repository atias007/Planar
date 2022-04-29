using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliAddJobRequest
    {
        [ActionProperty(Default = true)]
        public string Filename { get; set; }
    }
}