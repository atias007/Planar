using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Planar.Service.Model
{
    [Index("Username", Name = "IX_Users", IsUnique = true)]
    public partial class User
    {
        public User()
        {
            Groups = new HashSet<Group>();
        }

        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Unicode(false)]
        public string Username { get; set; }

        [Required]
        [MaxLength(128)]
        public byte[] Password { get; set; }

        [Required]
        [MaxLength(128)]
        public byte[] Salt { get; set; }

        public int RoleId { get; set; }

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

        [ForeignKey("UserId")]
        [InverseProperty("Users")]
        public virtual ICollection<Group> Groups { get; set; }
    }
}