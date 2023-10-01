using System;

namespace Planar.API.Common.Entities
{
    public class CounterRequest : IDateScope
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}