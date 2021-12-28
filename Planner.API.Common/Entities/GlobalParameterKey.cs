using Planner.API.Common.Validation;

namespace Planner.API.Common.Entities
{
    public class GlobalParameterKey
    {
        [Trim]
        [Required]
        public string Key { get; set; }
    }
}