using System.Collections.Generic;

namespace Planner.Service.Model
{
    public partial class Group
    {
        public IList<User> Users { get; set; }
    }
}