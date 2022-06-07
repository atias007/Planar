using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace Planar.Service.Model
{
    [Table("JobInstanceLog")]
    public partial class JobInstanceLog
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [StringLength(250)]
        public string InstanceId { get; set; }
        [Required]
        [StringLength(20)]
        public string JobId { get; set; }
        [Required]
        [StringLength(50)]
        public string JobName { get; set; }
        [Required]
        [StringLength(50)]
        public string JobGroup { get; set; }
        [Required]
        [StringLength(20)]
        public string TriggerId { get; set; }
        [Required]
        [StringLength(50)]
        public string TriggerName { get; set; }
        [Required]
        [StringLength(50)]
        public string TriggerGroup { get; set; }
        public int Status { get; set; }
        [StringLength(10)]
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
