using System.ComponentModel.DataAnnotations;

namespace Planar.API.Common.Entities
{
    public class GlobalConfigKey
    {
        [Required]
        public string Key { get; set; }
    }
}