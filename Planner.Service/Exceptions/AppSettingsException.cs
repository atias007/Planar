using System;

namespace Planner.Service.Exceptions
{
    public class AppSettingsException : Exception
    {
        public AppSettingsException(string message) : base(message)
        {
        }
    }
}