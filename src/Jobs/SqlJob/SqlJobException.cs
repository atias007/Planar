using Planar.Common.Exceptions;

namespace Planar;

public sealed class SqlJobException : PlanarException
{
    public SqlJobException(string message) : base(message)
    {
    }

    public SqlJobException(string message, Exception innerException) : base(message, innerException)
    {
    }
}