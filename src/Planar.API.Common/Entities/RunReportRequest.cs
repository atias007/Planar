using Planar.API.Common.Entities;
using System;

namespace Planar.Api.Common.Entities
{
    public class RunReportRequest : IDateScope
    {
        public string? Period { get; set; }
        public string? Group { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}