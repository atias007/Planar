namespace Planar.API.Common.Entities
{
    public class LoginRequest
    {
        public string Username { get; set; } = null!;

        public string Password { get; set; } = null!;
    }
}