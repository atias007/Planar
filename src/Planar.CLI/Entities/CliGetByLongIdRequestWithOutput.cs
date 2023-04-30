using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliGetByLongIdRequestWithOutput : CliOutputFilenameRequest
    {
        [ActionProperty(DefaultOrder = 0)]
        [Required("id argument is required")]
        public long Id { get; set; }
    }
}