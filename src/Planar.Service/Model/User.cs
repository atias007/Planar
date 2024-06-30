using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Planar.Service.Model;

[Index("Username", Name = "IX_Users", IsUnique = true)]
public partial class User
{
    [Key]
    public int Id { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string Username { get; set; } = null!;

    [MaxLength(128)]
    public byte[] Password { get; set; } = null!;

    [MaxLength(128)]
    public byte[] Salt { get; set; } = null!;

    [StringLength(50)]
    public string FirstName { get; set; } = null!;

    [StringLength(50)]
    public string? LastName { get; set; }

    [StringLength(250)]
    public string? EmailAddress1 { get; set; }

    [StringLength(250)]
    public string? EmailAddress2 { get; set; }

    [StringLength(250)]
    public string? EmailAddress3 { get; set; }

    [StringLength(50)]
    public string? PhoneNumber1 { get; set; }

    [StringLength(50)]
    public string? PhoneNumber2 { get; set; }

    [StringLength(50)]
    public string? PhoneNumber3 { get; set; }

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

    [ForeignKey("UserId")]
    [InverseProperty("Users")]
    public virtual ICollection<Group> Groups { get; set; } = new List<Group>();
}