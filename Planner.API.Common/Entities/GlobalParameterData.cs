using Planner.API.Common.Validation;

namespace Planner.API.Common.Entities
{
    public class GlobalParameterData : GlobalParameterKey
    {
        [Trim]
        [Required]
        public string Value { get; set; }
    }
}