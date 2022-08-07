using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliUpdateEntityRequest
    {
        [ActionProperty(DefaultOrder = 1)]
        [Required("id parameter is required")]
        public int Id { get; set; }

        [ActionProperty(DefaultOrder = 2)]
        [Required("propery name parameter is required")]
        public string PropertyName { get; set; }

        [ActionProperty(DefaultOrder = 3)]
        [Required("propery value parameter is required")]
        public string PropertyValue { get; set; }
    }
}