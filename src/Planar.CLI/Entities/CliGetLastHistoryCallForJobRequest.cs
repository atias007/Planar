using Planar.API.Common.Entities;
using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliGetLastHistoryCallForJobRequest : IPagingRequest
    {
        [ActionProperty(Default = true, Name = "last days")]
        public int LastDays { get; set; }

        public int? PageNumber { get; set; }

        public int? PageSize => 25;

        public void SetPagingDefaults()
        {
            PageNumber ??= 1;
        }
    }
}