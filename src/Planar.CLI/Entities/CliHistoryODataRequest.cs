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

    [ActionProperty("c", "count")]
    public bool? Count { get; set; }

    [ActionProperty("s", "select")]
    public string? Select { get; set; }
}