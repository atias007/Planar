using Planar.API.Common.Entities;
using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliPagingRequest(int pageSize) : IPagingRequest
    {
        public CliPagingRequest() : this(25)
        {
        }

        [ActionProperty("pn", "page-number")]
        public int? PageNumber { get; set; }

        [ActionProperty("ps", "page-size")]
        public int? PageSize { get; set; } = pageSize;

        public void SetPagingDefaults()
        {
            PageNumber ??= 1;
        }
    }
}