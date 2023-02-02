using System.Collections.Generic;

namespace Planar.API.Common.Entities
{
    public class CounterResponse
    {
        public List<StatisticsCountItem> Counter { get; set; } = new();
    }
}