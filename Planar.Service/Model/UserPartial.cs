using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Planar.Service.Model
{
    public partial class User
    {
        [NotMapped]
        public IList<Group> Groups { get; set; }
    }
}