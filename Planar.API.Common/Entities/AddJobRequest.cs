using System.ComponentModel.DataAnnotations;

namespace Planar.API.Common.Entities
{
    public class AddJobRequest
    {
        [Required]
        public string Yaml { get; set; }

        [Required]
        public string Path { get; set; }
    }
}