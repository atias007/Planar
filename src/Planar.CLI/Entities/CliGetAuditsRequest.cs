using Planar.API.Common.Entities;
using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliGetAuditsRequest : IPagingRequest
    {
        [ActionProperty(DefaultOrder = 0)]
        public int? PageNumber { get; set; }

        public int? PageSize => 25;

        public void SetPagingDefaults()
        {
            PageNumber ??= 1;
        }
    }
}