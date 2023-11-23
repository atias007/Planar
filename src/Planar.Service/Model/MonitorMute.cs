using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Planar.Service.Model;

[Table("MonitorMute")]
public partial class MonitorMute
{
    [Key]
    public int Id { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public string? JobId { get; set; }

    public int? MonitorId { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? DueDate { get; set; }
}
