using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliSetUserPasswordRequest : CliByNameRequest
    {
        [ActionProperty(DefaultOrder = 1)]
        [Required("password argument is required")]
        public string Password { get; set; } = null!;
    }
}