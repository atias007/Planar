using System;

namespace Planar.CLI.Entities;

public interface ICliDateScope
{
    DateTime FromDate { get; set; }
    DateTime ToDate { get; set; }
}