namespace Planar.Client.Entities
{
    public class LoginDetails
    {
        public string Role { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string? LastName { get; set; }
    }
}