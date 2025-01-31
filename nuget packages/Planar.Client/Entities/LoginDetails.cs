namespace Planar.Client.Entities
{
    public class LoginDetails
    {
#if NETSTANDARD2_0
        public string Role { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
#else
        public string Role { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string? LastName { get; set; }
#endif
    }
}