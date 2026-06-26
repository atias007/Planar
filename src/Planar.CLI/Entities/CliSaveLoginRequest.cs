using Planar.CLI.Attributes;
using System;

namespace Planar.CLI.Entities;

public class CliSaveLoginRequest
{
    [ActionProperty(LongName = "name", ShortName = "n")]
    public string? DisplayName { get; set; }

    [ActionProperty(LongName = "expire", ShortName = "e")]
    public DateTime? Expire { get; set; }
}