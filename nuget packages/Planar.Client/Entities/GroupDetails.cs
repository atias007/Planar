using System.Collections.Generic;

namespace Planar.Client.Entities
{
    public class GroupDetails : Group
    {
        private readonly List<UserMostBasicDetails> _users = new List<UserMostBasicDetails>();
        public IEnumerable<UserMostBasicDetails> Users => _users;

        internal void AddUser(UserMostBasicDetails user)
        {
            _users.Add(user);
        }
    }
}