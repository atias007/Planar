using Planner.CLI.Attributes;

namespace Planner.CLI.Entities
{
    public class CliAddJobRequest
    {
        [ActionProperty(Default = true)]
        public string Filename { get; set; }
    }
}