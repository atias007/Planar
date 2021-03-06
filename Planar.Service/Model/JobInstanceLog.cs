using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Planar.Service.Model
{
    [Table("JobInstanceLog")]
    public partial class JobInstanceLog
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [StringLength(250)]
        [Unicode(false)]
        public string InstanceId { get; set; }
        [Required]
        [StringLength(20)]
        [Unicode(false)]
        public string JobId { get; set; }
        [Required]
        [StringLength(50)]
        [Unicode(false)]
        public string JobName { get; set; }
        [Required]
        [StringLength(50)]
        [Unicode(false)]
        public string JobGroup { get; set; }
        [Required]
        [StringLength(20)]
        [Unicode(false)]
        public string TriggerId { get; set; }
        [Required]
        [StringLength(50)]
        [Unicode(false)]
        public string TriggerName { get; set; }
        [Required]
        [StringLength(50)]
        [Unicode(false)]
        public string TriggerGroup { get; set; }
        [StringLength(50)]
        public string ServerName { get; set; }
        public int Status { get; set; }
        [StringLength(10)]
        [Unicode(false)]
        public string StatusTitle { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime StartDate { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? EndDate { get; set; }
        public int? Duration { get; set; }
        public int? EffectedRows { get; set; }
        [StringLength(4000)]
        public string Data { get; set; }
        public string Information { get; set; }
        public string Exception { get; set; }
        public bool Retry { get; set; }
        public bool IsStopped { get; set; }
    }
}
