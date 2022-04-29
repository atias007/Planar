using Planar.API.Common.Validation;

namespace Planar.API.Common.Entities
{
    public class GlobalParameterKey
    {
        [Trim]
        [Required]
        public string Key { get; set; }
    }
}