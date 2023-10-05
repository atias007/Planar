using System.Collections.Generic;

namespace Planar.API.Common.Entities
{
    public class WorkingHoursDayModel
    {
        public string DayOfWeek { get; set; } = null!;
        public List<WorkingHourScopeModel> Scopes { get; set; } = null!;
    }
}