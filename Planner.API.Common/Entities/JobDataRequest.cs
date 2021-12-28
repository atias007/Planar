using Planner.API.Common.Validation;

namespace Planner.API.Common.Entities
{
    public class JobDataRequest : JobOrTriggerKey
    {
        [Trim]
        [Required]
        public string DataKey { get; set; }

        [Trim]
        public string DataValue { get; set; }
    }
}