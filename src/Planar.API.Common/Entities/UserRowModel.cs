namespace Planar.API.Common.Entities
{
    public class UserRowModel
    {
        public string? EmailAddress1 { get; set; }
        public string FirstName { get; set; } = null!;
        public int Id { get; set; }
        public string? LastName { get; set; }
        public string? PhoneNumber1 { get; set; }
        public string Username { get; set; } = null!;

        override public string ToString()
        {
            return $"{FirstName} {LastName} ({Username})";
        }
    }
}