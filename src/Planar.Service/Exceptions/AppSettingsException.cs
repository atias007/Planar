using System;

namespace Planar.Service.Exceptions
{
    [Serializable]
    public class AppSettingsException : Exception
    {
        public AppSettingsException(string message) : base(message)
        {
        }
    }
}