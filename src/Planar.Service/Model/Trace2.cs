using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Planar.Service.Model;

[Table("Trace2")]
public partial class Trace2
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("RenderedMessage")]
    public string? Message { get; set; }

    [StringLength(128)]
    public string? Level { get; set; }

    [Column("Timestamp")]
    public DateTime TimeStamp { get; set; }

    public string? Exception { get; set; }

    [Column("Properties")]
    public string? LogEvent { get; set; }
}