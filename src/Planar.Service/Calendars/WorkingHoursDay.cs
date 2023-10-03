using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Serialization;

namespace Planar.Service.Calendars;

public class WorkingHoursDay
{
    private string? _dayOfWeek;

    [YamlMember(Alias = "day of week")]
    public string? DayOfWeek
    {
        get { return _dayOfWeek; }
        set
        {
            _dayOfWeek = value;
            var temp = value?.ToLower().Replace(" ", string.Empty);
            if (string.IsNullOrWhiteSpace(temp)) { return; }
            if (Enum.TryParse<WorkingHoursDayType>(temp, ignoreCase: true, out var dayOfWeekMember))
            {
                DayOfWeekMember = dayOfWeekMember;
            }
        }
    }

    [YamlIgnore]
    public WorkingHoursDayType DayOfWeekMember { get; private set; }

    [YamlMember(Alias = "default scopes")]
    public bool DefaultScopes { get; set; }

    public List<WorkingHourScope> Scopes { get; set; } = new();

    public bool DayOff => !DefaultScopes && !Scopes.Any();

    public override string ToString()
    {
        var message = DefaultScopes ? nameof(DefaultScopes) : $"{Scopes?.Count ?? 0} Scopes";
        return $"{DayOfWeekMember}, {message}";
    }
}