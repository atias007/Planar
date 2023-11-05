namespace Planar.Service.Reports
{
    internal static class ReportConsts
    {
        // example: invoke wuyjsa0d455 --data report.group=admins --data report.period=qurterly

        internal const string EnableTriggerDataKey = "report.enable";
        internal const string GroupTriggerDataKey = "report.group";
        internal const string FromDateDataKey = "report.from.date";
        internal const string ToDateDataKey = "report.to.date";
        internal const string PeriodDataKey = "report.period";
    }
}