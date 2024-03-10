using System;

namespace Planar.Service.Calendars
{
    [Serializable]
    public sealed class PlanarCalendarException : Exception
    {
        public PlanarCalendarException(string message) : base(message)
        {
        }
    }
}