using System;

namespace Planar.API.Common.Entities
{
    public class SecurityAuditModel
    {
        public string Title { get; set; } = null!;

        public string Username { get; set; } = null!;

        public string UserTitle { get; set; } = null!;

        public DateTime DateCreated { get; set; }

        public bool IsWarning { get; set; }
    }
}