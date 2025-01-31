namespace Planar.Client.Entities
{
    public class User : UserMostBasicDetails
    {
#if NETSTANDARD2_0
        public string EmailAddress1 { get; set; }

        public string EmailAddress2 { get; set; }

        public string EmailAddress3 { get; set; }

        public string PhoneNumber1 { get; set; }

        public string PhoneNumber2 { get; set; }

        public string PhoneNumber3 { get; set; }

        public string AdditionalField1 { get; set; }

        public string AdditionalField2 { get; set; }

        public string AdditionalField3 { get; set; }

        public string AdditionalField4 { get; set; }

        public string AdditionalField5 { get; set; }
#else
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
#endif
    }
}