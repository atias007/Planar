namespace Planner.Service.API
{
    public record UpdateEntityRecord(int Id, string PropertyName, string PropertyValue);

    public record UpsertGroupRecord(int Id, string Name);

    public record AddUserToGroupRecord(int GroupId, int UserId);
}