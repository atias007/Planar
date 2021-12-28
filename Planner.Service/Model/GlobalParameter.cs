using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace Planner.Service.Model
{
    public partial class GlobalParameter
    {
        [Key]
        [StringLength(50)]
        public string ParamKey { get; set; }
        [Required]
        [StringLength(500)]
        public string ParamValue { get; set; }
    }
}
