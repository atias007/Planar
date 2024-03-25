using Planar.Common.Exceptions;

namespace Planar
{
    public sealed class PlanarJobException(string message) : PlanarException(message)
    {
    }
}