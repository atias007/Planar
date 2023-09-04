namespace Planar.Client
{
    internal class LoginResponse
    {
        public string Role { get; set; } = null!;
        public string Token { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string? LastName { get; set; }
    }
}