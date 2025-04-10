using Planar.CLI.Attributes;
using System;

namespace Planar.CLI.Entities;

public class CliResumeRequest : CliJobKey
{
    [ActionProperty("i", "in")]
    public TimeSpan? In { get; set; }
}