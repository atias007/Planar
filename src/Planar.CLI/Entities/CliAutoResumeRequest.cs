using Planar.CLI.Attributes;
using System;

namespace Planar.CLI.Entities;

public class CliAutoResumeRequest : CliJobKey
{
    [ActionProperty(DefaultOrder = 1, Name = CliActionMetadata.TimeSpan)]
    [Required("In argument is required")]
    public TimeSpan? In { get; set; }
}