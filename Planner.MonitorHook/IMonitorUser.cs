namespace Planner.MonitorHook
{
    public interface IMonitorUser
    {
        string EmailAddress1 { get; set; }
        string EmailAddress2 { get; set; }
        string EmailAddress3 { get; set; }
        string FirstName { get; set; }
        int Id { get; set; }
        string LastName { get; set; }
        string PhoneNumber1 { get; set; }
        string PhoneNumber2 { get; set; }
        string PhoneNumber3 { get; set; }
        string Reference1 { get; set; }
        string Reference2 { get; set; }
        string Reference3 { get; set; }
        string Reference4 { get; set; }
        string Reference5 { get; set; }
        string Username { get; set; }
    }
}