using System;

namespace Planar.API.Common.Entities;

public interface ICliDateScope
{
    DateTime FromDate { get; set; }
    DateTime ToDate { get; set; }
}