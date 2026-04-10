using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Planar.Service.Model;

[PrimaryKey("MonitorId", "Hook")]
public partial class MonitorActionsHook
{
    [Key]
    public int MonitorId { get; set; }

    [Key]
    [StringLength(50)]
    public string Hook { get; set; } = null!;

    [ForeignKey("MonitorId")]
    [InverseProperty("MonitorActionsHooks")]
    public virtual MonitorAction Monitor { get; set; } = null!;
}
