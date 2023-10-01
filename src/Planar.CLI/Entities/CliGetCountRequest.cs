using Planar.API.Common.Entities;
using Planar.CLI.Attributes;
using System;

namespace Planar.CLI.Entities;

public class CliGetCountRequest : ICliDateScope
{
    [ActionProperty("f", "from")]
    public DateTime FromDate { get; set; }

    [ActionProperty("t", "to")]
    public DateTime ToDate { get; set; }
}