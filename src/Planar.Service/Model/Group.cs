using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Planar.Service.Model;

[Index("Name", Name = "IX_Groups", IsUnique = true)]
public partial class Group
{
    [Key]
    public int Id { get; set; }

    [StringLength(50)]
    public string Name { get; set; } = null!;

    [StringLength(500)]
    public string? AdditionalField1 { get; set; }

    [StringLength(500)]
    public string? AdditionalField2 { get; set; }

    [StringLength(500)]
    public string? AdditionalField3 { get; set; }

    [StringLength(500)]
    public string? AdditionalField4 { get; set; }

    [StringLength(500)]
    public string? AdditionalField5 { get; set; }

    public int RoleId { get; set; }

    [InverseProperty("Group")]
    public virtual ICollection<MonitorAction> MonitorActions { get; set; } = new List<MonitorAction>();

    [ForeignKey("RoleId")]
    [InverseProperty("Groups")]
    public virtual Role Role { get; set; } = null!;

    [ForeignKey("GroupId")]
    [InverseProperty("Groups")]
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
