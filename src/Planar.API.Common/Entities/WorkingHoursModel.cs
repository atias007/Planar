using System.Collections.Generic;

namespace Planar.API.Common.Entities
{
    public class WorkingHoursModel
    {
        public string CalendarName { get; set; } = null!;
        public List<WorkingHoursDayModel> Days { get; set; } = new();
    }
}