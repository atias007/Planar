using System;

namespace Planar.API.Common.Entities;

public class UpdateGroupRequest : AddGroupRequest
{
    public string CurrentName { get; set; } = null!;

    public bool IsNameChanged => !string.Equals(CurrentName, Name, StringComparison.OrdinalIgnoreCase);
}