using System;

namespace Planar.API.Common.Entities
{
    public class CliDateScope : ICliDateScope
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
    }
}