namespace Planar.API.Common.Entities
{
    public class EntityTitle
    {
        public EntityTitle()
        {
            Username = string.Empty;
        }

        public EntityTitle(string name)
        {
            Username = name;
        }

        public EntityTitle(string username, string firstName, string? lastName) : this(username)
        {
            LastName = lastName;
            FirstName = firstName;
        }

        public string Username { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        public override string ToString()
        {
            var result = Username;
            if (!string.IsNullOrEmpty(FirstName) || !string.IsNullOrEmpty(LastName))
            {
                result += $" ({FirstName} {LastName})".TrimEnd();
            }

            return result;
        }
    }
}