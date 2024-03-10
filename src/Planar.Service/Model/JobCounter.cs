using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Planar.Service.Model;

[Table("JobCounters", Schema = "Statistics")]
[Index("RunDate", "JobId", Name = "IX_JobCounters", IsUnique = true)]
public partial class JobCounter
{
    [Key]
    public int Id { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public string JobId { get; set; } = null!;

    public DateOnly RunDate { get; set; }

    public int TotalRuns { get; set; }

    public int? SuccessRetries { get; set; }

    public int? FailRetries { get; set; }

    public int? Recovers { get; set; }
}
