using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Planar.Service.Model
{
    public partial class ClusterNode
    {
        [Key]
        [StringLength(100)]
        public string Server { get; set; }
        [Key]
        public short Port { get; set; }
        [Key]
        [StringLength(100)]
        public string InstanceId { get; set; }
        public short ClusterPort { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime JoinDate { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? HealthCheckDate { get; set; }
    }
}
