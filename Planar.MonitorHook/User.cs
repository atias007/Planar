#nullable disable

namespace Planar.MonitorHook
{
    public class User : IMonitorUser
    {
        public int Id { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string EmailAddress1 { get; set; }

        public string EmailAddress2 { get; set; }

        public string EmailAddress3 { get; set; }

        public string PhoneNumber1 { get; set; }

        public string PhoneNumber2 { get; set; }

        public string PhoneNumber3 { get; set; }

        public string Reference1 { get; set; }

        public string Reference2 { get; set; }

        public string Reference3 { get; set; }

        public string Reference4 { get; set; }

        public string Reference5 { get; set; }
    }
}