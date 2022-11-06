using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Planar.Service.Model
{
    [Table("GlobalConfig")]
    public partial class GlobalConfig
    {
        [Key]
        [StringLength(50)]
        public string Key { get; set; }

        [Required]
        [StringLength(1000)]
        public string Value { get; set; }

        [Required]
        [StringLength(10)]
        [Unicode(false)]
        public string Type { get; set; }
    }
}