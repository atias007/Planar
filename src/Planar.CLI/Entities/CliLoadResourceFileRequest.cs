using Planar.CLI.Attributes;

namespace Planar.CLI.Entities;

public class CliLoadResourceFileRequest
{
    [ActionProperty(DefaultOrder = 0, Name = "name")]
    [Required("name argument is required")]
    public string Name { get; set; } = string.Empty;

    [ActionProperty(DefaultOrder = 1, Name = "file")]
    [Required("file argument is required")]
    public string File { get; set; } = string.Empty;
}