namespace Planar.API.Common.Entities;

public class GlobalConfigModelAddRequest
{
    public required string Key { get; set; } = null!;
    public string? Value { get; set; }
    public string? Type { get; set; } = null!;
    public string? SourceUrl { get; set; }
}