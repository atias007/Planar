using System;

namespace Planner.Service.Exceptions
{
    internal class AppSettingsException : Exception
    {
        public AppSettingsException(string message) : base(message)
        {
        }
    }
}