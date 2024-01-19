using System;

namespace Planar.Client.Entities
{
    public interface IDateScope
    {
        DateTime FromDate { get; set; }
        DateTime ToDate { get; set; }
    }
}