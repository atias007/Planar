using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Planar.Service.Model;

public partial class MonitorAlert
{
    [Key]
    public int Id { get; set; }

    public int MonitorId { get; set; }

    [StringLength(50)]
    public string MonitorTitle { get; set; } = null!;

    public int EventId { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string EventTitle { get; set; } = null!;

    [StringLength(50)]
    [Unicode(false)]
    public string? EventArgument { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? JobName { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? JobGroup { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public string? JobId { get; set; }

    public int GroupId { get; set; }

    [StringLength(50)]
    public string GroupName { get; set; } = null!;

    public int UsersCount { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string Hook { get; set; } = null!;

    [StringLength(250)]
    [Unicode(false)]
    public string? LogInstanceId { get; set; }

    public bool HasError { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime AlertDate { get; set; }

    public string? Exception { get; set; }

    public string? AlertPayload { get; set; }
}