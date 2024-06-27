using System.ComponentModel;

namespace Planar.Service.Reports
{
    public enum ReportNames
    {
        [Description("Job Summary")]
        Summary,

        [Description("Paused Jobs")]
        Paused,

        [Description("Alerts")]
        Alerts,

        [Description("Audits")]
        Audits,

        [Description("Trace")]
        Trace
    }
}