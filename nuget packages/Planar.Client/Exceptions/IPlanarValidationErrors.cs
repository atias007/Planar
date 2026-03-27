using Planar.Client.Entities;
using System.Collections.Generic;

namespace Planar.Client.Exceptions
{
    public interface IPlanarValidationErrors
    {
        string Detail { get; }
        IEnumerable<PlanarError> Errors { get; }
        string Instance { get; }
        int? Status { get; }
        string Title { get; }
        string Type { get; }

#if NETSTANDARD2_0
        string ErrorCode { get; }
#else
       string? ErrorCode { get; }
#endif
    }
}