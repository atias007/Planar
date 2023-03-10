using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Planar.Service.Model
{
    public partial class Role
    {
        public Role()
        {
            Groups = new HashSet<Group>();
        }

        [Key]
        public int Id { get; set; }
        [StringLength(50)]
        [Unicode(false)]
        public string Name { get; set; } = null!;

        [InverseProperty("Role")]
        public virtual ICollection<Group> Groups { get; set; }
    }
}
