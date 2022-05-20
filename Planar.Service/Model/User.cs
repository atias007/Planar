﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace Planar.Service.Model
{
    public partial class User
    {
        public User()
        {
            UsersToGroups = new HashSet<UsersToGroup>();
        }

        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; }

        [Required]
        [StringLength(12)]
        public string Password { get; set; }

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; }

        [StringLength(50)]
        public string LastName { get; set; }

        [StringLength(250)]
        public string EmailAddress1 { get; set; }

        [StringLength(250)]
        public string EmailAddress2 { get; set; }

        [StringLength(250)]
        public string EmailAddress3 { get; set; }

        [StringLength(50)]
        public string PhoneNumber1 { get; set; }

        [StringLength(50)]
        public string PhoneNumber2 { get; set; }

        [StringLength(50)]
        public string PhoneNumber3 { get; set; }

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

        [InverseProperty(nameof(UsersToGroup.User))]
        public virtual ICollection<UsersToGroup> UsersToGroups { get; set; }
    }
}