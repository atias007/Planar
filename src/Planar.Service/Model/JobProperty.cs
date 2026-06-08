using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Planar.Service.Model;

public partial class JobProperty
{
    [Key]
    [StringLength(20)]
    [Unicode(false)]
    public string JobId { get; set; } = null!;

    public string? Properties { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string JobType { get; set; } = null!;

    [StringLength(2000)]
    [Unicode(false)]
    public string? GlobalConfigKeys { get; set; }
}
