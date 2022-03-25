using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace Planner.Service.Model
{
    public partial class MonitorAction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Title { get; set; }

        public int EventId { get; set; }

        [StringLength(50)]
        public string EventArgument { get; set; }

        [StringLength(20)]
        public string JobId { get; set; }

        [StringLength(50)]
        public string JobGroup { get; set; }

        public int GroupId { get; set; }

        [Required]
        [StringLength(50)]
        public string Hook { get; set; }

        [Required]
        public bool? Active { get; set; }

        [ForeignKey(nameof(GroupId))]
        [InverseProperty("MonitorActions")]
        public virtual Group Group { get; set; }
    }
}