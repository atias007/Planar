using Planar.MonitorHook;
using Planar.Service.Model;

namespace Planar.Service.Monitor
{
    public partial class MonitorUser : IMonitorUser
    {
        public MonitorUser(User user)
        {
            Id = user.Id;
            Username = user.Username;
            FirstName = user.FirstName;
            LastName = user.LastName;
            EmailAddress1 = user.EmailAddress1;
            EmailAddress2 = user.EmailAddress2;
            EmailAddress3 = user.EmailAddress3;
            PhoneNumber1 = user.PhoneNumber1;
            PhoneNumber2 = user.PhoneNumber2;
            PhoneNumber3 = user.PhoneNumber3;
            Reference1 = user.Reference1;
            Reference2 = user.Reference2;
            Reference3 = user.Reference3;
            Reference4 = user.Reference4;
            Reference5 = user.Reference5;
        }

        public int Id { get; set; }
        public string Username { get; set; }
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