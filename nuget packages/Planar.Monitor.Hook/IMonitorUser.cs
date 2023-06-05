namespace Planar.Monitor.Hook
{
    public interface IMonitorUser
    {
        string? EmailAddress1 { get; }
        string? EmailAddress2 { get; }
        string? EmailAddress3 { get; }
        string FirstName { get; }
        int Id { get; }
        string? LastName { get; }
        string? PhoneNumber1 { get; }
        string? PhoneNumber2 { get; }
        string? PhoneNumber3 { get; }
        string? AdditionalField1 { get; }
        string? AdditionalField2 { get; }
        string? AdditionalField3 { get; }
        string? AdditionalField4 { get; }
        string? AdditionalField5 { get; }
        string Username { get; }
    }
}