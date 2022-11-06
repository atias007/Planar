using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Planar.Service.Model
{
    public partial class MonitorAction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Title { get; set; }

        public int EventId { get; set; }

        [StringLength(50)]
        [Unicode(false)]
        public string EventArgument { get; set; }

        [StringLength(20)]
        [Unicode(false)]
        public string JobId { get; set; }

        [StringLength(50)]
        [Unicode(false)]
        public string JobGroup { get; set; }

        public int GroupId { get; set; }

        [Required]
        [StringLength(50)]
        [Unicode(false)]
        public string Hook { get; set; }

        [Required]
        public bool? Active { get; set; }

        [ForeignKey("GroupId")]
        [InverseProperty("MonitorActions")]
        public virtual Group Group { get; set; }
    }
}