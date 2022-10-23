using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliGetByIdRequest
    {
        [ActionProperty(DefaultOrder = 0)]
        [Required("id parameter is required")]
        public int Id { get; set; }
    }
}