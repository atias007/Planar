using System;

namespace CommonJob;

public sealed class JobMonitorException(string message, Exception innerException) : Exception(message, innerException)
{
}