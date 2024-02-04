using System;

namespace Planar.Client.Entities
{
    public class SecurityAuditDetails
    {
        public string Title { get; set; } = null!;

        public string Username { get; set; } = null!;

        public string UserTitle { get; set; } = null!;

        public DateTime DateCreated { get; set; }

        public bool IsWarning { get; set; }
    }
}