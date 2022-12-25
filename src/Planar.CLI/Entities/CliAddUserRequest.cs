using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliAddUserRequest
    {
        [ActionProperty("u", "username")]
        [Required]
        public string Username { get; set; }

        [ActionProperty("f", "firstname")]
        public string FirstName { get; set; }

        [ActionProperty("l", "lastname")]
        public string LastName { get; set; }

        [ActionProperty("e", "email")]
        public string Email { get; set; }

        [ActionProperty("p", "phone-number")]
        public string PhoneNumber { get; set; }
    }
}