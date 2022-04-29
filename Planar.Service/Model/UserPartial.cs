using System.Collections.Generic;

namespace Planar.Service.Model
{
    public partial class User
    {
        public IList<Group> Groups { get; set; }
    }
}