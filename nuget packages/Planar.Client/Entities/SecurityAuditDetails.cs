using System;

namespace Planar.Client.Entities
{
    public class SecurityAuditDetails
    {
#if NETSTANDARD2_0
        public string Title { get; set; }
        public string Username { get; set; }
        public string UserTitle { get; set; }
#else
        public string Title { get; set; } = null!;
        public string Username { get; set; } = null!;
        public string UserTitle { get; set; } = null!;
#endif

        public DateTime DateCreated { get; set; }

        public bool IsWarning { get; set; }
    }
}