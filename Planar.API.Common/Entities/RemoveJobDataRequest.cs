using Planar.API.Common.Validation;

namespace Planar.API.Common.Entities
{
    public class RemoveJobDataRequest : JobOrTriggerKey
    {
        [Trim]
        [Required]
        public string DataKey { get; set; }
    }
}