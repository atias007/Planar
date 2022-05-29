using System.ComponentModel.DataAnnotations;

#nullable disable

namespace Planar.Service.Model
{
    public partial class GlobalParameter
    {
        [Key]
        [StringLength(50)]
        public string ParamKey { get; set; }

        [Required]
        [StringLength(500)]
        public string ParamValue { get; set; }
    }
}