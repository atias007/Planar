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

        [ActionProperty("l", "lastname", Name = "last name")]
        public string? LastName { get; set; }

        [ActionProperty("e", "email", Name = "email address")]
        public string? EmailAddress1 { get; set; }

        [ActionProperty("p", "phone-number", Name = "phone number")]
        public string? PhoneNumber1 { get; set; }
    }

    public class CliUpdateUserRequest : CliAddUserRequest
    {
        [ActionProperty("e2", "email2", Name = "email address 2")]
        public string? EmailAddress2 { get; set; }

        [ActionProperty("e3", "email3", Name = "email address 3")]
        public string? EmailAddress3 { get; set; }

        [ActionProperty("p2", "phone-number2", Name = "phone number 2")]
        public string? PhoneNumber2 { get; set; }

        [ActionProperty("p3", "phone-number3", Name = "phone number 3")]
        public string? PhoneNumber3 { get; set; }

        [ActionProperty(ShortName = "f1", LongName = "field1", Name = "additional field 1")]
        public string? AdditionalField1 { get; set; }

        [ActionProperty(ShortName = "f2", LongName = "field2", Name = "additional field 2")]
        public string? AdditionalField2 { get; set; }

        [ActionProperty(ShortName = "f3", LongName = "field3", Name = "additional field 3")]
        public string? AdditionalField3 { get; set; }

        [ActionProperty(ShortName = "f4", LongName = "field4", Name = "additional field 4")]
        public string? AdditionalField4 { get; set; }

        [ActionProperty(ShortName = "f5", LongName = "field5", Name = "additional field 5")]
        public string? AdditionalField5 { get; set; }
    }
}