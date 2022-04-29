using Planar.API.Common.Validation;

namespace Planar.API.Common.Entities
{
    public class AddJobRequest
    {
        [Trim]
        [Required]
        public string Yaml { get; set; }

        [Trim]
        [Required]
        public string Path { get; set; }
    }
}