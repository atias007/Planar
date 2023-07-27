using System.Collections.Generic;
using System.Security.Claims;

namespace Planar.Service.Audit
{
    public class SecurityMessage
    {
        public string Title { get; set; } = null!;
        public bool IsWarning { get; set; }
        public IEnumerable<Claim>? Claims { get; set; }
    }
}