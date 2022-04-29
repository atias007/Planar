using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace Planar.Service.Model
{
    public partial class UsersToGroup
    {
        [Key]
        public int UserId { get; set; }

        [Key]
        public int GroupId { get; set; }

        [ForeignKey(nameof(GroupId))]
        [InverseProperty("UsersToGroups")]
        public virtual Group Group { get; set; }

        [ForeignKey(nameof(UserId))]
        [InverseProperty("UsersToGroups")]
        public virtual User User { get; set; }
    }
}