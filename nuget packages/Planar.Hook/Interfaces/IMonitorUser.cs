namespace Planar.Hook
{
    public interface IMonitorUser
    {
        int Id { get; }
        string FirstName { get; }
        string Username { get; }

#if NETSTANDARD2_0
        string EmailAddress1 { get; }
        string EmailAddress2 { get; }
        string EmailAddress3 { get; }
        string LastName { get; }
        string PhoneNumber1 { get; }
        string PhoneNumber2 { get; }
        string PhoneNumber3 { get; }
        string AdditionalField1 { get; }
        string AdditionalField2 { get; }
        string AdditionalField3 { get; }
        string AdditionalField4 { get; }
        string AdditionalField5 { get; }
#else
        string? EmailAddress1 { get; }
        string? EmailAddress2 { get; }
        string? EmailAddress3 { get; }
        string? LastName { get; }
        string? PhoneNumber1 { get; }
        string? PhoneNumber2 { get; }
        string? PhoneNumber3 { get; }
        string? AdditionalField1 { get; }
        string? AdditionalField2 { get; }
        string? AdditionalField3 { get; }
        string? AdditionalField4 { get; }
        string? AdditionalField5 { get; }
#endif
    }
}