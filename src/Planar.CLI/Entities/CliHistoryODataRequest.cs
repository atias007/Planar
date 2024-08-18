using Planar.CLI.Attributes;

namespace Planar.CLI.Entities;

public class CliHistoryODataRequest
{
    [ActionProperty("f", "filter")]
    public string? Filter { get; set; }

    [ActionProperty("o", "order")]
    public string? OrderBy { get; set; }

    [ActionProperty("t", "top")]
    public int? Top { get; set; }

    [ActionProperty("k", "skip")]
    public int? Skip { get; set; }

    [ActionProperty("s", "select")]
    public string? Select { get; set; }

    [ActionProperty("m", "metadata")]
    public bool? Metadata { get; set; }
}