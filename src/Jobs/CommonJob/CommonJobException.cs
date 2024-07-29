using System;

namespace CommonJob;

public sealed class CommonJobException(string message, Exception innerException) :
    Exception(message, innerException)
{
}