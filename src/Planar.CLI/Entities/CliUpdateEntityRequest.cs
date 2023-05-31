using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliUpdateEntityRequest
    {
        [ActionProperty(DefaultOrder = 1, Name = "property name")]
        [Required("property name argument is required")]
        public string PropertyName { get; set; } = string.Empty;

        [ActionProperty(DefaultOrder = 2, Name = "value")]
        [Required("value argument is required")]
        public string PropertyValue { get; set; } = string.Empty;
    }
}