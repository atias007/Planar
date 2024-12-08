using System;

namespace Planar.API.Common.Entities;

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

    public string? Description { get; set; }

    public JobActiveMembers Active { get; set; }

    public DateTime? AutoResume { get; set; }

    public bool EqualsId(string id)
    {
        if (Id == null || id == null)
        {
            return false;
        }

        return Id.Equals(id, StringComparison.OrdinalIgnoreCase) || $"{Group}.{Name}".Equals(id, StringComparison.OrdinalIgnoreCase);
    }
}