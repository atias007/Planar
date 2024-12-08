using Planar.CLI.Attributes;
using System;

namespace Planar.CLI.Entities;

public class CliAutoResumeRequest : CliJobKey
{
    [ActionProperty("a", "at")]
    public TimeSpan? At { get; set; }
}