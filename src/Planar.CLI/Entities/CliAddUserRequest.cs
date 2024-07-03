using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliAddUserRequest
    {
        [ActionProperty("u", "username")]
        [Required("username argument is required")]
        public string Username { get; set; } = string.Empty;

        [ActionProperty("f", "firstname", Name = "first name")]
        [Required("firstname argument is required")]
        public string FirstName { get; set; } = string.Empty;

        [ActionProperty("l", "lastname", Name = "first name")]
        public string? LastName { get; set; }

        [ActionProperty("e", "email")]
        public string? EmailAddress1 { get; set; }

        [ActionProperty("p", "phone-number", Name = "phone numbers")]
        public string? PhoneNumber1 { get; set; }
    }
}