using System.ComponentModel.DataAnnotations;

namespace Planar.API.Common.Entities
{
    public class AddUserRequest
    {
        [Required]
        [Range(2, 50)]
        public string Username { get; set; }

        [Required]
        [Range(2, 50)]
        public string FirstName { get; set; }

        [Range(2, 50)]
        public string LastName { get; set; }

        [EmailAddress]
        [Range(5, 250)]
        public string Email { get; set; }

        [Range(9, 50)]
        public string PhoneNumber { get; set; }
    }
}