using System;

namespace Planar.Client.Entities
{
    internal interface IDateScope
    {
        DateTime? FromDate { get; set; }
        DateTime? ToDate { get; set; }
    }
}