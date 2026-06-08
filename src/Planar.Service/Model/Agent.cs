using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Planar.Service.Model;

public partial class Agent
{
    [Key]
    [StringLength(100)]
    public string ClientId { get; set; } = null!;

    [StringLength(50)]
    [Unicode(false)]
    public string IpAddress { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime LastSeen { get; set; }
}
