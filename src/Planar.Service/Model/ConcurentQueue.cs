using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Planar.Service.Model;

[Table("ConcurentQueue", Schema = "Statistics")]
public partial class ConcurentQueue
{
    [Key]
    public long Id { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime RecordDate { get; set; }

    [StringLength(100)]
    public string Server { get; set; } = null!;

    [StringLength(100)]
    public string InstanceId { get; set; } = null!;

    public int ConcurentValue { get; set; }
}
