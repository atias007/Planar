using System.Collections.Generic;

namespace Planar.Client.Entities
{
    public class CounterResponse
    {
        public List<StatisticsCountItem> Counter { get; set; } = new List<StatisticsCountItem>();
    }
}