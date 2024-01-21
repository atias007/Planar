using System;

namespace Planar.Client.Entities
{
    public class CounterFilter : IDateScope
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}