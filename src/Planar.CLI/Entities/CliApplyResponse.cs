using System.Collections.Generic;

namespace Planar.CLI.Entities;

internal class CliApplyResponse
{
    public int TotalUpdate { get; set; }
    public int TotalAdd { get; set; }
    public int TotalDelete { get; set; }
    public int TotalUnchanged { get; set; }
    public List<CliApplyResponseItem> Items { get; set; } = [];
}

public class CliApplyResponseItem
{
    public required string Key { get; set; }
    public required string Action { get; set; }
    public int ActionId { get; set; }
    public required string Description { get; set; }
    public string? Source { get; set; }
}