using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Planar.Service.Model;

public partial class MonitorAction
{
    [Key]
    public int Id { get; set; }

    [StringLength(50)]
    public string Title { get; set; } = null!;

    public int EventId { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? EventArgument { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? JobName { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? JobGroup { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string Hook { get; set; } = null!;

    public bool Active { get; set; }

    [ForeignKey("MonitorId")]
    [InverseProperty("Monitors")]
    public virtual ICollection<Group> Groups { get; set; } = new List<Group>();
}
