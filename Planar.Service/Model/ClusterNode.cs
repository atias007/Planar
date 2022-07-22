using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Planar.Service.Model
{
    [Index("InstanceId", Name = "IX_ClusterNodes", IsUnique = true)]
    public partial class ClusterNode
    {
        [Key]
        [StringLength(100)]
        public string Server { get; set; }

        [Key]
        public short Port { get; set; }

        [Required]
        [StringLength(100)]
        public string InstanceId { get; set; }

        public short ClusterPort { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime JoinDate { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime? HealthCheckDate { get; set; }
    }
}