using Planar.CLI.Attributes;

namespace Planar.CLI.Entities;

public class CliApplyRequest
{
    [ActionProperty(DefaultOrder = 0)]
    [Required("filename argument is required")]
    public string Filename { get; set; } = string.Empty;
}