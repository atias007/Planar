using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliAddJobRequest
    {
        [ActionProperty(Default = true)]
        [Required]
        public string Filename { get; set; }
    }
}