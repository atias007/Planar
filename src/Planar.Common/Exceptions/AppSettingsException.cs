using System;

namespace Planar.Common.Exceptions
{
    public sealed class AppSettingsException(string message) : Exception(message)
    {
    }
}