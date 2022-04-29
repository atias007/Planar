using Planar.API.Common.Validation;

namespace Planar.API.Common.Entities
{
    public class GlobalParameterData : GlobalParameterKey
    {
        [Trim]
        [Required]
        public string Value { get; set; }
    }
}