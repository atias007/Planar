using Microsoft.Extensions.DependencyInjection;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Service.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planar.Service.Reports
{
    public sealed class AuditsReport(IServiceScopeFactory serviceScope) : BaseReport(serviceScope)
    {
        public override string ReportName => "Audits";

        public override async Task<string> Generate(DateScope dateScope)
        {
            var auditsTask = GetAudits(dateScope);
            var auditsTable = GetAuditsTable(await auditsTask);

            var main = GetMainTemplate();

            main = ReplacePlaceHolder(main, "ReportPeriod", dateScope.Period);
            main = ReplacePlaceHolder(main, "ReportDate.From", dateScope.From.ToShortDateString());
            main = ReplacePlaceHolder(main, "ReportDate.To", dateScope.To.ToShortDateString());
            main = ReplacePlaceHolder(main, "Environment", AppSettings.General.Environment);
            main = ReplacePlaceHolder(main, "AuditsTable", auditsTable);

            return main;
        }

        private string GetAuditsTable(IEnumerable<JobAuditDto> data)
        {
            if (!data.Any()) { return EmptyTableHtml; }

            var rows = new StringBuilder();
            foreach (var item in data)
            {
                var rowTemplate = GetResource("audits_row");
                rowTemplate = ReplacePlaceHolder(rowTemplate, "Id", item.Id.ToString());
                rowTemplate = ReplacePlaceHolder(rowTemplate, "DateCreated", $"{item.DateCreated.ToShortDateString()} {item.DateCreated:HH:mm:ss}");
                rowTemplate = ReplacePlaceHolder(rowTemplate, "JobId", item.JobId);
                rowTemplate = ReplacePlaceHolder(rowTemplate, "JobKey", item.JobKey, encode: true);
                rowTemplate = ReplacePlaceHolder(rowTemplate, "User", GetUser(item), encode: true);
                rowTemplate = ReplacePlaceHolder(rowTemplate, "Description", item.Description, encode: true);
                rows.AppendLine(rowTemplate);
            }

            var table = GetResource("audits_table");
            table = ReplacePlaceHolder(table, "AuditRow", rows.ToString());
            return table;
        }

        private static string GetUser(JobAuditDto jobAudit)
        {
            if (string.Equals(jobAudit.UserTitle, jobAudit.Username, StringComparison.InvariantCulture))
            {
                return jobAudit.Username;
            }

            return $"{jobAudit.UserTitle} ({jobAudit.Username})";
        }

        private async Task<IEnumerable<JobAuditDto>> GetAudits(DateScope dateScope)
        {
            using var scope = ServiceScope.CreateScope();
            var jobDomain = scope.ServiceProvider.GetRequiredService<JobDomain>();
            var result = await jobDomain.GetAuditsForReport(dateScope);
            return result;
        }
    }
}