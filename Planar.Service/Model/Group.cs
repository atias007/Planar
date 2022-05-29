using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace Planar.Service.Model
{
    [Index(nameof(Name), Name = "IX_Groups", IsUnique = true)]
    public partial class Group
    {
        public Group()
        {
            MonitorActions = new HashSet<MonitorAction>();
            UsersToGroups = new HashSet<UsersToGroup>();
        }

        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        [StringLength(500)]
        public string Reference1 { get; set; }

        [StringLength(500)]
        public string Reference2 { get; set; }

        [StringLength(500)]
        public string Reference3 { get; set; }

        [StringLength(500)]
        public string Reference4 { get; set; }

        [StringLength(500)]
        public string Reference5 { get; set; }

        [InverseProperty(nameof(MonitorAction.Group))]
        public virtual ICollection<MonitorAction> MonitorActions { get; set; }

        [InverseProperty(nameof(UsersToGroup.Group))]
        public virtual ICollection<UsersToGroup> UsersToGroups { get; set; }
    }
}