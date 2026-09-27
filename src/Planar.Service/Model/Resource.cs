using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Planar.Service.Model;

public partial class Resource
{
    [Key]
    [StringLength(100)]
    public string Name { get; set; } = null!;

    public string Value { get; set; } = null!;
}
