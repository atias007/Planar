using Planar.CLI.Attributes;
using System;

namespace Planar.CLI.Entities;

public class CliPauseRequest : CliJobKey
{
    [ActionProperty("f", "for")]
    public TimeSpan? For { get; set; }
}