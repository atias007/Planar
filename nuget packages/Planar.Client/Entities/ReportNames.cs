using System.ComponentModel;

namespace Planar.Client.Entities
{
    public enum ReportNames
    {
        [Description("Job Summary")]
        Summary,

        [Description("Paused Jobs")]
        Paused,

        [Description("Alerts")]
        Alerts,

        [Description("Trace")]
        Trace
    }
}