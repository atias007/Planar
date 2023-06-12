using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliGetByIdRequestWithOutput
    {
        [ActionProperty(DefaultOrder = 0)]
        [Required("id argument is required")]
        public int Id { get; set; }
    }
}