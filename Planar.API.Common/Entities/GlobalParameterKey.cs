using System.ComponentModel.DataAnnotations;

namespace Planar.API.Common.Entities
{
    public class GlobalParameterKey
    {
        [Required]
        public string Key { get; set; }
    }
}