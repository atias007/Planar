using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliGetJobFileRequest
    {
        [ActionProperty(DefaultOrder = 0, LongName = "jobfile")]
        [Required("name parameter is required")]
        public string JobFile { get; set; }
    }
}