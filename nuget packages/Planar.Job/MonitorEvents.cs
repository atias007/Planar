using System.ComponentModel;

namespace Planar.Job
{
    public enum CustomMonitorEvents
    {
        [Description("Custom event 1")] CustomEvent1 = 400,
        [Description("Custom event 2")] CustomEvent2 = 401,
        [Description("Custom event 3")] CustomEvent3 = 402,
        [Description("Custom event 4")] CustomEvent4 = 403,
        [Description("Custom event 5")] CustomEvent5 = 404,
    }
}