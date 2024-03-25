using Planar.Common.Exceptions;

namespace Planar
{
    public sealed class PlanarJobException : PlanarException
    {
        public PlanarJobException(string message) : base(message)
        {
        }
    }
}