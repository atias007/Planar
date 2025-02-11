using System;

namespace Planar.Client.Entities
{
    public class JobAudit
    {
        public int Id { get; set; }

#if NETSTANDARD2_0
        public string JobId { get; set; }

        public string JobKey { get; set; }

        public string Username { get; set; }

        public string UserTitle { get; set; }

        public string Description { get; set; }
#else
        public string JobId { get; set; } = null!;

        public string JobKey { get; set; } = null!;

        public string Username { get; set; } = null!;

        public string UserTitle { get; set; } = null!;

        public string Description { get; set; } = null!;
#endif

        public DateTime DateCreated { get; set; }
    }
}