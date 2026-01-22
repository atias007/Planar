namespace Planar.API.Common.Entities;

public class GlobalConfigModelAddRequest
{
    public required string Key { get; set; }
    public string? Value { get; set; }
    public string? Type { get; set; }
    public string? SourceUrl { get; set; }
}