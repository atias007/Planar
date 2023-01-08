using System;
using System.Runtime.Serialization;

namespace Planar.Calendar
{
    [Serializable]
    public class PlanarCalendarException : Exception
    {
        public PlanarCalendarException(string message) : base(message)
        {
        }

        protected PlanarCalendarException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}