using System;

namespace Planar.API.Common.Entities
{
    public class JobAuditDto
    {
        public int Id { get; set; }

        public string JobId { get; set; } = null!;

        public string JobKey { get; set; } = null!;

        public DateTime DateCreated { get; set; }

        public string Username { get; set; } = null!;

        public string UserTitle { get; set; } = null!;

        public string Description { get; set; } = null!;
    }
}