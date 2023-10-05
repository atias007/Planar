using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Planar.Service.Calendars;

public class WorkingHoursCalendar
{
    [YamlMember(Alias = "calendar name")]
    public string CalendarName { get; set; } = string.Empty;

    [YamlMember(Alias = "default scopes")]
    public List<WorkingHourScope> DefaultScopes { get; set; } = new();

    public List<WorkingHoursDay> Days { get; set; } = new();

    public WorkingHoursDay? WorkingHourDay(WorkingHoursDayType dayType)
    {
        return Days.Find(d => d.DayOfWeekMember == dayType);
    }
}