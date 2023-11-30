namespace Planar.Monitor.Hook
{
    internal class User : IMonitorUser
    {
        public int Id { get; set; }

        public string Username { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

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