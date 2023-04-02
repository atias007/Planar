using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Planar.Service.Model
{
    [Table("JobDurationStatistics", Schema = "Statistics")]
    public partial class JobDurationStatistic
    {
        [Key]
        [StringLength(20)]
        [Unicode(false)]
        public string JobId { get; set; } = null!;
        [Column(TypeName = "numeric(18, 4)")]
        public decimal AvgDuration { get; set; }
        [Column(TypeName = "numeric(18, 4)")]
        public decimal StdevDuration { get; set; }
        public int Rows { get; set; }
    }
}
