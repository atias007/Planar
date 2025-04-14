using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Planar.Service.Model;

[Table("HistoryLastLog")]
public partial class HistoryLastLog
{
    public long Id { get; set; }

    [StringLength(250)]
    [Unicode(false)]
    public string InstanceId { get; set; } = null!;

    [Key]
    [StringLength(20)]
    [Unicode(false)]
    public string JobId { get; set; } = null!;

    [StringLength(50)]
    [Unicode(false)]
    public string JobName { get; set; } = null!;

    [StringLength(50)]
    [Unicode(false)]
    public string JobGroup { get; set; } = null!;

    [StringLength(50)]
    [Unicode(false)]
    public string JobType { get; set; } = null!;

    [StringLength(20)]
    [Unicode(false)]
    public string TriggerId { get; set; } = null!;

    [StringLength(50)]
    public string? ServerName { get; set; }

    public int Status { get; set; }

    [StringLength(10)]
    [Unicode(false)]
    public string? StatusTitle { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime StartDate { get; set; }

    public int? Duration { get; set; }

    public int? EffectedRows { get; set; }

    public bool Retry { get; set; }

    public bool IsCanceled { get; set; }

    public bool HasWarnings { get; set; }
}
