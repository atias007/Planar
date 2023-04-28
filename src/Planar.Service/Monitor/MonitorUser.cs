using Planar.Service.Model;

namespace Planar.Service.Monitor
{
    public partial class MonitorUser
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
            AdditionalField1 = user.AdditionalField1;
            AdditionalField2 = user.AdditionalField2;
            AdditionalField3 = user.AdditionalField3;
            AdditionalField4 = user.AdditionalField4;
            AdditionalField5 = user.AdditionalField5;
        }

        public int Id { get; set; }
        public string Username { get; set; }
        public string FirstName { get; set; }
        public string? LastName { get; set; }
        public string? EmailAddress1 { get; set; }
        public string? EmailAddress2 { get; set; }
        public string? EmailAddress3 { get; set; }
        public string? PhoneNumber1 { get; set; }
        public string? PhoneNumber2 { get; set; }
        public string? PhoneNumber3 { get; set; }
        public string? AdditionalField1 { get; set; }
        public string? AdditionalField2 { get; set; }
        public string? AdditionalField3 { get; set; }
        public string? AdditionalField4 { get; set; }
        public string? AdditionalField5 { get; set; }
    }
}