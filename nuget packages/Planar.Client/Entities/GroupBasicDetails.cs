namespace Planar.Client.Entities
{
    public class GroupBasicDetails
    {
#if NETSTANDARD2_0
        public string Name { get; set; }

#else
        public string? Name { get; set; }

#endif

        public int UsersCount { get; set; }

        public Roles Role { get; set; }
    }
}