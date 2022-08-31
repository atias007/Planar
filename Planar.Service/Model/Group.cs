using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Planar.Service.Model
{
    [Index("Name", Name = "IX_Groups", IsUnique = true)]
    public partial class Group
    {
        public Group()
        {
            MonitorActions = new HashSet<MonitorAction>();
            Users = new HashSet<User>();
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
        public int RoleId { get; set; }

        [InverseProperty("Group")]
        public virtual ICollection<MonitorAction> MonitorActions { get; set; }

        [ForeignKey("GroupId")]
        [InverseProperty("Groups")]
        public virtual ICollection<User> Users { get; set; }
    }
}
