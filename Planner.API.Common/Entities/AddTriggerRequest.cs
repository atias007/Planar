using Planner.API.Common.Validation;

namespace Planner.API.Common.Entities
{
    public class AddTriggerRequest : JobOrTriggerKey
    {
        [Trim]
        [Required]
        public string Yaml { get; set; }
    }
}