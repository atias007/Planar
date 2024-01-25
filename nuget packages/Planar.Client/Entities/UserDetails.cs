using System.Collections.Generic;

namespace Planar.Client.Entities
{
    public class UserDetails : User
    {
        private readonly List<string> _groups = new List<string>();
        public IEnumerable<string> Groups => _groups;

        internal void AddGroup(string group)
        {
            _groups.Add(group);
        }
    }
}