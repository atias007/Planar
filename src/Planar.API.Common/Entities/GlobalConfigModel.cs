using System;

namespace Planar.API.Common.Entities;

public class GlobalConfigModel
{
    public required string Key { get; set; } = null!;
    public required string? Value { get; set; }
    public required string Type { get; set; } = null!;
    public string? SourceUrl { get; set; }
    public DateTime? LastUpdate { get; set; }
}