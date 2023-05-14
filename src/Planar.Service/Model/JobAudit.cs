using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Planar.Service.Model;

[Table("JobAudit")]
public partial class JobAudit
{
    [Key]
    public int Id { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public string JobId { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime DateCreated { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string Username { get; set; } = null!;

    [StringLength(101)]
    public string UserTitle { get; set; } = null!;

    [StringLength(200)]
    [Unicode(false)]
    public string Description { get; set; } = null!;

    [StringLength(4000)]
    public string? AdditionalInfo { get; set; }
}
