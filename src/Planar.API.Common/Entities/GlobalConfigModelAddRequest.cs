namespace Planar.API.Common.Entities;

public class GlobalConfigModelUpdateRequest
{
    public required string Key { get; set; }
    public string? Value { get; set; }
    public string? SourceUrl { get; set; }
}

public class GlobalConfigModelAddRequest : GlobalConfigModelUpdateRequest
{
    public string? Type { get; set; }

    public bool? IsSecret { get; set; }
}