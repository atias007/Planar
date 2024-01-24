using System.Collections.Generic;

namespace Planar.Client.Entities
{
    public class GroupDetails : Group
    {
        private readonly List<UserBasicDetails> _users = new List<UserBasicDetails>();
        public IEnumerable<UserBasicDetails> Users => _users;

        internal void AddUser(UserBasicDetails user)
        {
            _users.Add(user);
        }
    }
}