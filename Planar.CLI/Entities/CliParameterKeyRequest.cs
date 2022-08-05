using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliParameterKeyRequest
    {
        [ActionProperty(DefaultOrder = 0)]
        [Required("key parameter is required")]
        public string Key { get; set; }
    }
}