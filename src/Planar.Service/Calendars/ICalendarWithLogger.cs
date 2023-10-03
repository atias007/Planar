using Microsoft.Extensions.Logging;

namespace Planar.Service.Calendars
{
    public interface ICalendarWithLogger
    {
        ILogger? Logger { get; set; }
    }
}