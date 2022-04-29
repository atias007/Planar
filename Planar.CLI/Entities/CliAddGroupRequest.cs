using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliAddGroupRequest
    {
        [ActionProperty(ShortName = "n", LongName = "name", Default = true)]
        public string Name { get; set; }
    }
}