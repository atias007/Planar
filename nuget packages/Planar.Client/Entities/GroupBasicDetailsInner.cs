using System;

namespace Planar.Client.Entities
{
    internal class GroupBasicDetailsInner
    {
        public string? Name { get; set; }

        public int UsersCount { get; set; }

        public string? Role { get; set; }

        public GroupBasicDetails GetGroupBasicDetails()
        {
            var result = new GroupBasicDetails
            {
                Name = Name,
                UsersCount = UsersCount
            };

            if (Enum.TryParse<Roles>(Role, true, out var role))
            {
                result.Role = role;
            }
            else
            {
                result.Role = Roles.Anonymous;
            }

            return result;
        }
    }
}