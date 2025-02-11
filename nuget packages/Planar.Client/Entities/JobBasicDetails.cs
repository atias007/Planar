using System;

namespace Planar.Client.Entities
{
    public enum JobActiveMembers
    {
        Active,
        PartiallyActive,
        Inactive
    }

    public class JobBasicDetails
    {
        public string Id { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string JobType { get; set; } = string.Empty;
#if NETSTANDARD2_0
        public string Description { get; set; }
#else
        public string? Description { get; set; }
#endif
        public JobActiveMembers Active { get; set; }
        public DateTime? AutoResume { get; set; }
    }
}