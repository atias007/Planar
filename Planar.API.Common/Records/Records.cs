namespace Planar
{
    // TODO: verify if needed all this records

    public record UpdateEntityRecord(int Id, string PropertyName, string PropertyValue);

    public record UpsertGroupRecord(int Id, string Name);

    public record UserToGroupRecord(int UserId);
}