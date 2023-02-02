using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliAddGroupRequest
    {
        [ActionProperty(ShortName = "n", LongName = "name", Default = true)]
        [Required("name argument is required")]
        public string Name { get; set; } = string.Empty;
    }
}