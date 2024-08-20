using System.Collections.Generic;

namespace Planar.Client.Entities
{
    public class GroupDetails : Group
    {
        public IEnumerable<UserMostBasicDetails> Users { get; internal set; } = new List<UserBasicDetails>();
    }
}