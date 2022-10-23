using System;

namespace Planar.Calendar
{
    [Serializable]
    public class PlanarCalendarException : Exception
    {
        public PlanarCalendarException(string message) : base(message)
        {
        }
    }
}