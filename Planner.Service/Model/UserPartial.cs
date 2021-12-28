using System.Collections.Generic;

namespace Planner.Service.Model
{
    public partial class User
    {
        public IList<Group> Groups { get; set; }
    }
}