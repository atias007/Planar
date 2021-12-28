using Planner.API.Common.Validation;

namespace Planner.API.Common.Entities
{
    public class AddUserRequest
    {
        [Trim]
        [Required]
        [Length(2, 50)]
        public string Username { get; set; }

        [Trim]
        [Required]
        [Length(2, 50)]
        public string FirstName { get; set; }

        [Trim]
        [Length(2, 50)]
        public string LastName { get; set; }

        [Trim]
        [Email]
        [Length(5, 250)]
        public string Email { get; set; }

        [Trim]
        [Length(9, 50)]
        [Numeric]
        public string PhoneNumber { get; set; }
    }
}