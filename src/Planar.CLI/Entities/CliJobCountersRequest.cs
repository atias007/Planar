using Planar.CLI.Attributes;
using System;

namespace Planar.CLI.Entities
{
    public class CliJobCountersRequest
    {
        [ActionProperty(DefaultOrder = 0, Name = "from date")]
        public DateTime FromDate { get; set; }
    }
}