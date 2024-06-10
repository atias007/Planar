using Planar.Common.Exceptions;

namespace Planar;

public sealed class SqlTableReportJobException : PlanarException
{
    public SqlTableReportJobException(string message) : base(message)
    {
    }

    public SqlTableReportJobException(string message, Exception innerException) : base(message, innerException)
    {
    }
}