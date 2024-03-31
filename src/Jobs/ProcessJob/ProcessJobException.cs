using Planar.Common.Exceptions;

namespace ProcessJob
{
    public sealed class ProcessJobException(string message) : PlanarException(message)
    {
    }
}