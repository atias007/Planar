using Planar.CLI.Attributes;

namespace Planar.CLI.Entities;

public class CliRunningLogRequest : CliFireInstanceIdRequest
{
    [ActionProperty("l", "live")]
    public bool Live { get; set; }
}