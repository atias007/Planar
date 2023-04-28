namespace Planar.Monitor.Hook
{
    public interface IMonitorUser
    {
        string? EmailAddress1 { get; set; }
        string? EmailAddress2 { get; set; }
        string? EmailAddress3 { get; set; }
        string FirstName { get; set; }
        int Id { get; set; }
        string? LastName { get; set; }
        string? PhoneNumber1 { get; set; }
        string? PhoneNumber2 { get; set; }
        string? PhoneNumber3 { get; set; }
        string? AdditionalField1 { get; set; }
        string? AdditionalField2 { get; set; }
        string? AdditionalField3 { get; set; }
        string? AdditionalField4 { get; set; }
        string? AdditionalField5 { get; set; }
        string Username { get; set; }
    }
}