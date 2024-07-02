namespace Planar.Service.Model.DataObjects
{
    public class UserIdentity
    {
        public required int Id { get; set; }
        public required string Username { get; set; }
        public string Role { get; set; } = null!;

        public required string Surename { get; set; } = null!;
        public required string? GivenName { get; set; } = null!;

        public required byte[] Password { get; set; } = null!;

        public required byte[] Salt { get; set; } = null!;

        public string Fullname => $"{Surename} {GivenName}".Trim();
    }
}