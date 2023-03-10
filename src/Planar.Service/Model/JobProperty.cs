using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Planar.Service.Model
{
    public partial class JobProperty
    {
        [Key]
        [StringLength(20)]
        [Unicode(false)]
        public string JobId { get; set; } = null!;
        public string? Properties { get; set; }
    }
}
