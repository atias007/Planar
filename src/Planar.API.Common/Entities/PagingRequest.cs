using System.Net.Http.Headers;

namespace Planar.API.Common.Entities
{
    public abstract class PagingRequest : IPagingRequest
    {
        public int? PageNumber { get; set; }

        public int? PageSize { get; set; }

        public bool IsEmpty => PageNumber == null && PageSize == null;
    }
}