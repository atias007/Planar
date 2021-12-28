using Planner.API.Common.Validation;

namespace Planner.API.Common.Entities
{
    public class AddJobRequest
    {
        [Trim]
        [Required]
        public string Yaml { get; set; }
    }
}