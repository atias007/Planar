using System;

namespace Planar.Service.Calendars;

public sealed class PlanarCalendarException(string message) : Exception(message)
{
}