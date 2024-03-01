using Planar.Client.Entities;
using System.Collections.Generic;

namespace Planar.Client.Exceptions
{
    public interface IPlanarValidationErrors
    {
        string Detail { get; }
        int? ErrorCode { get; }
        IEnumerable<PlanarError> Errors { get; }
        string Instance { get; }
        int Status { get; }
        string Title { get; }
        string Type { get; }
    }
}