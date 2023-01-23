using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliUpdateEntityRequest
    {
        [ActionProperty(DefaultOrder = 1)]
        [Required("id argument is required")]
        public int Id { get; set; }

        [ActionProperty(DefaultOrder = 2)]
        [Required("propery name argument is required")]
        public string PropertyName { get; set; }

        [ActionProperty(DefaultOrder = 3)]
        [Required("propery value argument is required")]
        public string PropertyValue { get; set; }
    }
}