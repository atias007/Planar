using Planner.API.Common.Validation;

namespace Planner.API.Common.Entities
{
    public class RemoveJobDataRequest : JobOrTriggerKey
    {
        [Trim]
        [Required]
        public string DataKey { get; set; }
    }
}