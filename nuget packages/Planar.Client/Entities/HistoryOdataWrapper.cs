using System.Collections.Generic;

namespace Planar.Client.Entities
{
    internal class HistoryOdataWrapper
    {
        public IEnumerable<HistoryDetails> Value { get; set; } = new List<HistoryDetails>();
    }
}