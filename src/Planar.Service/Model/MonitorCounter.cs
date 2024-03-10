using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Planar.Service.Model;

[Index("JobId", "MonitorId", Name = "IX_MonitorCounters", IsUnique = true)]
public partial class MonitorCounter
{
    [Key]
    public int Id { get; set; }

    public int MonitorId { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public string JobId { get; set; } = null!;

    public int Counter { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? LastUpdate { get; set; }
}
