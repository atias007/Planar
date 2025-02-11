namespace Planar.Client
{
    internal class LoginResponse
    {
#if NETSTANDARD2_0
        public string Role { get; set; }
        public string Token { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
#else
        public string Role { get; set; } = null!;
        public string Token { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string? LastName { get; set; }
#endif
    }
}