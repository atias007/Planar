namespace Planar
{
    public record UpdateEntityRecord(string PropertyName, string PropertyValue);

    public record UpsertGroupRecord(int Id, string Name);

    public record UserToGroupRecord(int UserId);
}