namespace Planar.CLI.Entities
{
    public record UserRowDetails(int Id, string FirstName, string LastName, string Username, string EmailAddress1, string PhoneNumber1);

    public record GroupRowDetails(int Id, string Name);
}