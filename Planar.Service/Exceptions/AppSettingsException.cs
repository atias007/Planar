using System;

namespace Planar.Service.Exceptions
{
    public class AppSettingsException : Exception
    {
        public AppSettingsException(string message) : base(message)
        {
        }
    }
}