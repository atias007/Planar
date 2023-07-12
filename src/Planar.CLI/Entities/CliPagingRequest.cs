using Planar.API.Common.Entities;
using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public class CliPagingRequest : IPagingRequest
    {
        public CliPagingRequest() : this(25)
        {
        }

        public CliPagingRequest(int pageSize)
        {
            PageSize = pageSize;
        }

        [ActionProperty("pn", "page-number")]
        public int? PageNumber { get; set; }

        [ActionProperty("ps", "page-size")]
        public int? PageSize { get; set; }

        public void SetPagingDefaults()
        {
            PageNumber ??= 1;
        }
    }
}