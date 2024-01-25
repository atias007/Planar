using System;
using System.Collections.Generic;

namespace Planar.Client.Entities
{
    internal sealed class GroupInner
    {
        public string Name { get; set; } = null!;
        public string? AdditionalField1 { get; set; }
        public string? AdditionalField2 { get; set; }
        public string? AdditionalField3 { get; set; }
        public string? AdditionalField4 { get; set; }
        public string? AdditionalField5 { get; set; }
        public string? Role { get; set; }

        public List<UserMostBasicDetails> Users { get; set; } = new List<UserMostBasicDetails>();

        public GroupDetails GetGroupDetails()
        {
            var group = new GroupDetails
            {
                AdditionalField1 = AdditionalField1,
                AdditionalField2 = AdditionalField2,
                AdditionalField3 = AdditionalField3,
                AdditionalField4 = AdditionalField4,
                AdditionalField5 = AdditionalField5,
                Name = Name
            };

            if (Enum.TryParse<Roles>(Role, true, out var role))
            {
                group.Role = role;
            }

            Users?.ForEach(u =>
            {
                var user = new UserMostBasicDetails
                {
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Username = u.Username
                };

                group.AddUser(user);
            });

            return group;
        }
    }
}