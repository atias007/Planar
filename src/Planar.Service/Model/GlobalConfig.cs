using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Planar.Service.Model;

[Table("GlobalConfig")]
public partial class GlobalConfig
{
    [Key]
    [StringLength(50)]
    public string Key { get; set; } = null!;

    [StringLength(1000)]
    public string? Value { get; set; }

    [StringLength(10)]
    [Unicode(false)]
    public string Type { get; set; } = null!;
}
