using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliUpdateEntityRequest
    {
        [ActionProperty(DefaultOrder = 1)]
        public int Id { get; set; }

        [ActionProperty(DefaultOrder = 2)]
        public string PropertyName { get; set; }

        [ActionProperty(DefaultOrder = 3)]
        public string PropertyValue { get; set; }
    }
}