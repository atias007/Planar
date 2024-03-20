using Microsoft.Extensions.DependencyInjection;
using Planar.API.Common.Entities;
using Planar.Service.API;
using Planar.Service.Monitor;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planar.Service.Reports
{
    public sealed class AlertsReport(IServiceScopeFactory serviceScope) : BaseReport(serviceScope)
    {
        public override string ReportName => "Alerts";

        public override async Task<string> Generate(DateScope dateScope)
        {
            var alertsTask = GetAlerts(dateScope);
            var alertsTable = GetAlertsTable(await alertsTask);

            var main = GetMainTemplate();

            main = ReplacePlaceHolder(main, "ReportPeriod", dateScope.Period);
            main = ReplacePlaceHolder(main, "ReportDate.From", dateScope.From.ToShortDateString());
            main = ReplacePlaceHolder(main, "ReportDate.To", dateScope.To.ToShortDateString());
            main = ReplacePlaceHolder(main, "AlertsTable", alertsTable);

            return main;
        }

        private string GetAlertsTable(IEnumerable<MonitorAlertRowModel> data)
        {
            if (!data.Any()) { return EmptyTableHtml; }

            var rows = new StringBuilder();
            foreach (var item in data)
            {
                var rowTemplate = GetResource("alerts_row");
                rowTemplate = ReplacePlaceHolder(rowTemplate, "MonitorTitle", item.MonitorTitle, encode: true);
                rowTemplate = ReplacePlaceHolder(rowTemplate, "EventTitle", MonitorUtil.GetMonitorEventTitle(item.EventTitle, item.EventArgument), encode: true);
                rowTemplate = ReplacePlaceHolder(rowTemplate, "JobKey", GetJobKey(item), encode: true);
                rowTemplate = ReplacePlaceHolder(rowTemplate, "Group", item.GroupName, encode: true);
                rowTemplate = ReplacePlaceHolder(rowTemplate, "Hook", item.Hook, encode: true);
                rowTemplate = ReplacePlaceHolder(rowTemplate, "AlertDate", $"{item.AlertDate.ToShortDateString()} {item.AlertDate:HH:mm:ss}");
                rows.AppendLine(rowTemplate);
            }

            var table = GetResource("alerts_table");
            table = ReplacePlaceHolder(table, "AlertsRow", rows.ToString());
            return table;
        }

        private static string GetJobKey(MonitorAlertRowModel row)
        {
            if (string.IsNullOrWhiteSpace(row.JobGroup) && string.IsNullOrWhiteSpace(row.JobName))
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(row.JobGroup)) { return row.JobName ?? string.Empty; }

            return $"{row.JobGroup}.{row.JobName}";
        }

        private async Task<IEnumerable<MonitorAlertRowModel>> GetAlerts(DateScope dateScope)
        {
            using var scope = ServiceScope.CreateScope();
            var monitorData = scope.ServiceProvider.GetRequiredService<MonitorDomain>();
            var request = new GetMonitorsAlertsRequest
            {
                PageNumber = 1,
                PageSize = 1000,
                FromDate = dateScope.From,
                ToDate = dateScope.To,
                Ascending = true
            };

            var response = await monitorData.GetMonitorsAlerts(request);
            var result = response.Data ?? [];
            return result;
        }
    }
}