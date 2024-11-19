using Planar.CLI.Attributes;

namespace Planar.CLI.Entities;

public class CliSleepRequest
{
    [ActionProperty(DefaultOrder = 0)]
    public int Seconds { get; set; }
}