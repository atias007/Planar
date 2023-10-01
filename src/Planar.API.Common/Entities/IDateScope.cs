using System;

namespace Planar.API.Common.Entities
{
    public interface IDateScope
    {
        DateTime? FromDate { get; set; }
        DateTime? ToDate { get; set; }
    }
}