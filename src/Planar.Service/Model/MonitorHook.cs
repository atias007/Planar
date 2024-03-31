using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Planar.Service.Model;

[Index("Name", Name = "IX_MonitorHooks", IsUnique = true)]
public partial class MonitorHook
{
    [Key]
    public int Id { get; set; }

    [StringLength(50)]
    public string Name { get; set; } = null!;

    [StringLength(2000)]
    public string Description { get; set; } = null!;

    [StringLength(1000)]
    public string Path { get; set; } = null!;
}
