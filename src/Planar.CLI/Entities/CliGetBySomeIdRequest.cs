using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliGetBySomeIdRequest
    {
        [ActionProperty(DefaultOrder = 0)]
        [Required("id argument is required")]
        public string Id { get; set; } = null!;
    }
}