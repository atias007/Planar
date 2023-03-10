using System;
using System.Runtime.Serialization;

namespace Planar.Common.Exceptions
{
    [Serializable]
    public class AppSettingsException : Exception
    {
        public AppSettingsException(string message) : base(message)
        {
        }

        protected AppSettingsException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}