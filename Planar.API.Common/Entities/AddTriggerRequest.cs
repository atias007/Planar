using System.ComponentModel.DataAnnotations;

namespace Planar.API.Common.Entities
{
    public class AddTriggerRequest : JobOrTriggerKey
    {
        [Required]
        public string Yaml { get; set; }
    }
}