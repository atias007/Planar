namespace Planar.Client.Entities
{
    public class UserMostBasicDetails
    {
#if NETSTANDARD2_0
        public string Username { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
#else
        public string Username { get; set; } = null!;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
#endif
    }
}