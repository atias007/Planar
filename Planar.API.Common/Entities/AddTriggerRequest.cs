using Planar.API.Common.Validation;

namespace Planar.API.Common.Entities
{
    public class AddTriggerRequest : JobOrTriggerKey
    {
        [Trim]
        [Required]
        public string Yaml { get; set; }
    }
}