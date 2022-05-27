using System.ComponentModel.DataAnnotations;

namespace Planar.API.Common.Entities
{
    public class GlobalParameterData : GlobalParameterKey
    {
        [Required]
        public string Value { get; set; }
    }
}