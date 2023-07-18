using Planar.API.Common.Entities;
using Planar.CLI.CliGeneral;
using Planar.CLI.Entities;
using Spectre.Console;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Planar.CLI
{
    internal static class CliTableExtensions
    {
        public static CliTable GetTable(AddUserResponse? response)
        {
            var table = new CliTable();
            if (response == null) { return table; }
            table.Table.AddColumns("Password");
            table.Table.AddRow(SafeCliString(response.Password));
            table.Table.AddEmptyRow();
            table.Table.AddRow(CliFormat.GetWarningMarkup("make sure you copy the above password now."));
            table.Table.AddRow($"[{CliFormat.WarningColor}]we don't store it and you will not be able to see it again.[/]");
            return table;
        }

        public static CliTable GetTable(JobStatistic? response)
        {
            var table = new CliTable();
            if (response == null) { return table; }
            table.Table.AddColumns("Key", "Value");
            table.Table.AddRow("Average Duration", CliTableFormat.FormatTimeSpan(response.AvgDuration));
            table.Table.AddRow("Standard Deviation Duration", CliTableFormat.FormatTimeSpan(response.StdevDuration));
            table.Table.AddRow("Average Effected Rows", response.AvgEffectedRows.ToString("N2"));
            table.Table.AddRow("Standard Deviation Effected Rows", response.StdevEffectedRows.ToString("N2"));
            table.Table.AddRow("Total Runs", response.TotalRuns.ToString());
            table.Table.AddRow("Success Retries", response.SuccessRetries.ToString());
            table.Table.AddRow("Fail Retries", response.FailRetries.ToString());
            table.Table.AddRow("Recovers", response.Recovers.ToString());
            return table;
        }

        public static CliTable GetTable(PagingResponse<JobAuditDto>? response, bool withJobId = false)
        {
            var table = new CliTable(paging: response, "audit");
            if (response == null || response.Data == null) { return table; }

            if (withJobId)
            {
                table.Table.AddColumns("Id", "Job Id", "Job Key", "Date Created", "Username", "User Title", "Description");
            }
            else
            {
                table.Table.AddColumns("Id", "Date Created", "Username", "User Title", "Description");
            }

            foreach (var item in response.Data)
            {
                if (item == null) { continue; }
                if (withJobId)
                {
                    table.Table.AddRow(item.Id.ToString(), item.JobId.EscapeMarkup(), item.JobKey.EscapeMarkup(), CliTableFormat.FormatDateTime(item.DateCreated), item.Username, item.UserTitle, SafeCliString(item.Description));
                }
                else
                {
                    table.Table.AddRow(item.Id.ToString(), CliTableFormat.FormatDateTime(item.DateCreated), item.Username, item.UserTitle, SafeCliString(item.Description));
                }
            }

            return table;
        }

        public static CliTable GetTable(IEnumerable<KeyValueItem>? response)
        {
            var table = new CliTable(showCount: true);
            if (response == null) { return table; }
            table.Table.AddColumns("Key", "Value");
            foreach (var item in response)
            {
                if (item == null) { continue; }
                table.Table.AddRow(SafeCliString(item.Key), LimitValue(item.Value));
            }

            return table;
        }

        public static CliTable GetTable(IEnumerable<LovItem>? response, string entityName)
        {
            var table = new CliTable(showCount: true, entityName);
            if (response == null) { return table; }
            table.Table.AddColumns("Id", "Name");
            foreach (LovItem item in response)
            {
                table.Table.AddRow(item.Id.ToString(), LimitValue(item.Name));
            }

            return table;
        }

        public static CliTable GetTable(List<MonitorEventModel>? response)
        {
            var table = new CliTable(showCount: true, entityName: "event");
            if (response == null) { return table; }
            table.Table.AddColumns("Event Name", "Event Title");
            response.ForEach(r => table.Table.AddRow(r.EventName, r.EventTitle));
            return table;
        }

        public static CliTable GetTable(PagingResponse<JobRowDetails>? response)
        {
            var table = new CliTable(paging: response, entityName: "job");
            if (response == null || response.Data == null) { return table; }
            table.Table.AddColumns("Job Id", "Job Key", "Job Type", "Description");
            response.Data.ForEach(r => table.Table.AddRow(r.Id, CliTableFormat.FormatJobKey(r.Group, r.Name), r.JobType.EscapeMarkup(), LimitValue(r.Description)));
            return table;
        }

        public static CliTable GetTable(List<RunningJobDetails>? response)
        {
            var table = new CliTable(showCount: true, "job");
            if (response == null) { return table; }
            table.Table.AddColumns("Fire Instance Id", "Job Id", "Job Key", "Progress", "Effected Rows", "Ex. Count", "Run Time", "End Time");
            response.ForEach(r => table.Table.AddRow(
                CliTableFormat.GetFireInstanceIdMarkup(r.FireInstanceId),
                $"{r.Id}",
                CliTableFormat.FormatJobKey(r.Group, r.Name),
                CliTableFormat.GetProgressMarkup(r.Progress),
                $"{r.EffectedRows}",
                CliTableFormat.FormatExceptionCount(r.ExceptionsCount),
                CliTableFormat.FormatTimeSpan(r.RunTime),
                $"[grey]{CliTableFormat.FormatTimeSpan(r.EstimatedEndTime)}[/]"));
            return table;
        }

        public static CliTable GetTable(PagingResponse<JobInstanceLogRow>? response, bool singleJob = false)
        {
            var table = new CliTable(paging: response);
            if (response == null || response.Data == null) { return table; }

            if (singleJob)
            {
                table.Table.AddColumns("Id", "Trigger Id", "Status", "Start Date", "Duration", "Effected Rows");
                response.Data.ForEach(r => table.Table.AddRow($"{r.Id}", CliTableFormat.GetTriggerIdMarkup(r.TriggerId ?? string.Empty), CliTableFormat.GetStatusMarkup(r.Status), CliTableFormat.FormatDateTime(r.StartDate), CliTableFormat.FromatDuration(r.Duration), CliTableFormat.FormatNumber(r.EffectedRows)));
            }
            else
            {
                table.Table.AddColumns("Id", "Job Id", "Job Key", "Job Type", "Trigger Id", "Status", "Start Date", "Duration", "Effected Rows");
                response.Data.ForEach(r => table.Table.AddRow($"{r.Id}", r.JobId ?? string.Empty, CliTableFormat.FormatJobKey(r.JobGroup, r.JobName), r.JobType.EscapeMarkup(), CliTableFormat.GetTriggerIdMarkup(r.TriggerId ?? string.Empty), CliTableFormat.GetStatusMarkup(r.Status), CliTableFormat.FormatDateTime(r.StartDate), CliTableFormat.FromatDuration(r.Duration), CliTableFormat.FormatNumber(r.EffectedRows)));
            }

            return table;
        }

        public static CliTable GetTable(PagingResponse<JobHistory>? response)
        {
            var table = new CliTable(paging: response);
            if (response == null || response.Data == null) { return table; }
            table.Table.AddColumns("Id", "Job Id", "Job Key", "Job Type", "Trigger Id", "Status", "Start Date", "Duration", "Effected Rows");
            response.Data.ForEach(r => table.Table.AddRow($"{r.Id}", r.JobId ?? string.Empty, CliTableFormat.FormatJobKey(r.JobGroup, r.JobName), r.JobType.EscapeMarkup(), CliTableFormat.GetTriggerIdMarkup(r.TriggerId ?? string.Empty), CliTableFormat.GetStatusMarkup(r.Status), CliTableFormat.FormatDateTime(r.StartDate), CliTableFormat.FromatDuration(r.Duration), CliTableFormat.FormatNumber(r.EffectedRows)));
            return table;
        }

        public static CliTable GetTable(PagingResponse<LogDetails>? response)
        {
            var table = new CliTable(paging: response);
            if (response == null || response.Data == null) { return table; }
            table.Table.AddColumns("Id", "Message", "Level", "Time Stamp");
            response.Data.ForEach(r => table.Table.AddRow($"{r.Id}", SafeCliString(r.Message), CliTableFormat.GetLevelMarkup(r.Level), CliTableFormat.FormatDateTime(r.TimeStamp)));
            return table;
        }

        public static CliTable GetTable(TriggerRowDetails? response)
        {
            var table = new CliTable(showCount: true, entityName: "trigger");
            if (response == null) { return table; }
            table.Table.AddColumns("Trigger Id", "Trigger Name", "State", "Next Fire Time", "Interval/Cron");
            response.SimpleTriggers.ForEach(r => table.Table.AddRow($"{r.Id}", r.TriggerName.EscapeMarkup(), r.State ?? string.Empty, CliTableFormat.FormatDateTime(r.NextFireTime), CliTableFormat.FormatTimeSpan(r.RepeatInterval)));
            response.CronTriggers.ForEach(r => table.Table.AddRow($"{r.Id}", r.TriggerName.EscapeMarkup(), r.State ?? string.Empty, CliTableFormat.FormatDateTime(r.NextFireTime), r.CronExpression.EscapeMarkup()));
            return table;
        }

        public static List<CliTable> GetTable(JobDetails? response)
        {
            var table = new CliTable();
            if (response == null)
            {
                return new List<CliTable> { table };
            }

            table.Table.AddColumns("Property Name", "Value");
            table.Table.AddRow(nameof(response.Id), response.Id.EscapeMarkup());
            table.Table.AddRow(nameof(response.Group), response.Group.EscapeMarkup());
            table.Table.AddRow(nameof(response.Name), response.Name.EscapeMarkup());
            table.Table.AddRow(nameof(response.Author), response.Author.EscapeMarkup());
            table.Table.AddRow(nameof(response.JobType), response.JobType.EscapeMarkup());
            table.Table.AddRow(nameof(response.Description), response.Description.EscapeMarkup());
            table.Table.AddRow(nameof(response.Durable), response.Durable.ToString());
            table.Table.AddRow(nameof(response.RequestsRecovery), response.RequestsRecovery.ToString());
            table.Table.AddRow(nameof(response.Concurrent), response.Concurrent.ToString());

            var dataMap = SerializeJobDetailsData(response);

            table.Table.AddRow("Data", dataMap.EscapeMarkup() ?? string.Empty);
            table.Table.AddRow(nameof(response.Properties), response.Properties.EscapeMarkup());

            var response2 = new TriggerRowDetails
            {
                SimpleTriggers = response.SimpleTriggers,
                CronTriggers = response.CronTriggers
            };

            var table2 = GetTable(response2);

            return new List<CliTable> { table, table2 };
        }

        public static CliTable GetTable(List<CliClusterNode>? response)
        {
            var table = new CliTable(showCount: true, entityName: "node");
            if (response == null) { return table; }
            table.Table.AddColumns("Server", "Port", "Instance Id", "Cluster Port", "Join Date", "Health Check");
            response.ForEach(r =>
            {
                var hcTitle = CliTableFormat.FormatClusterHealthCheck(r.HealthCheckGap, r.HealthCheckGapDeviation);
                table.Table.AddRow(r.Server.EscapeMarkup(), r.Port.ToString(), r.InstanceId.EscapeMarkup(), r.ClusterPort.ToString(), CliTableFormat.FormatDateTime(r.JoinDate), hcTitle);
            });

            return table;
        }

        public static CliTable GetTable(PagingResponse<MonitorItem>? response)
        {
            var table = new CliTable(paging: response, entityName: "monitor");
            if (response == null || response.Data == null) { return table; }

            table.Table.AddColumns("Id", "Title", "Event", "Job Group", "Job Name", "Event Arguments", "Dist. Group", "Hook", "Active");
            response.Data.ForEach(r => table.Table.AddRow(
                r.Id.ToString(),
                r.Title.EscapeMarkup(),
                r.EventTitle.EscapeMarkup(),
                r.JobGroup.EscapeMarkup(),
                r.JobName.EscapeMarkup(),
                r.EventArgument.EscapeMarkup(),
                r.DistributionGroupName.EscapeMarkup(),
                r.Hook.EscapeMarkup(),
                CliTableFormat.GetBooleanMarkup(r.Active)));

            return table;
        }

        public static CliTable GetTable(PagingResponse<MonitorAlertRowModel>? response)
        {
            var table = new CliTable(paging: response, entityName: "alert");
            if (response == null || response.Data == null) { return table; }

            table.Table.AddColumns("Id", "Monitor Title", "Event Title", "Event Arguments", "Alert Date", "JobId", "Job Key", "Dist. Group", "Hook", "Has Error");
            response.Data.ForEach(r => table.Table.AddRow(
                r.Id.ToString(),
                r.MonitorTitle.EscapeMarkup(),
                r.EventTitle.EscapeMarkup(),
                r.EventArgument.EscapeMarkup(),
                CliTableFormat.FormatDateTime(r.AlertDate),
                r.JobId.EscapeMarkup(),
                CliTableFormat.FormatJobKey(r.JobGroup, r.JobName),
                r.GroupName.EscapeMarkup(),
                r.Hook.EscapeMarkup(),
                r.HasError.ToString()));

            return table;
        }

        public static CliTable GetTable(PagingResponse<GroupInfo>? response)
        {
            var table = new CliTable(paging: response, entityName: "group");
            if (response == null || response.Data == null) { return table; }
            table.Table.AddColumns("Name", "Role", "User Count");
            response.Data.ForEach(r => table.Table.AddRow(r.Name.EscapeMarkup(), r.Role.EscapeMarkup(), $"{r.UsersCount}"));
            return table;
        }

        public static CliTable GetTable(PagingResponse<UserRowDetails>? data)
        {
            var table = new CliTable(paging: data, entityName: "user");
            if (data == null || data.Data == null) { return table; }
            table.Table.AddColumns("Username", "First Name", "Last Name", "Email Address 1", "Phone Number 1");
            data.Data.ForEach(r => table.Table.AddRow(r.Username.EscapeMarkup(), r.FirstName.EscapeMarkup(), r.LastName.EscapeMarkup(), r.EmailAddress1.EscapeMarkup(), r.PhoneNumber1.EscapeMarkup()));
            return table;
        }

        public static CliTable GetTable(List<PausedTriggerDetails>? response)
        {
            var table = new CliTable(showCount: true, entityName: "trigger");
            if (response == null) { return table; }
            table.Table.AddColumns("Trigger Id", "Trigger Name", "Job Id", "Job Key");
            response.ForEach(r => table.Table.AddRow(r.Id.EscapeMarkup(), r.TriggerName.EscapeMarkup(), r.JobId.EscapeMarkup(), CliTableFormat.FormatJobKey(r.JobGroup, r.JobName)));
            return table;
        }

        internal static CliTable GetTable(List<CliGlobalConfig>? response)
        {
            var table = new CliTable(showCount: true);
            if (response == null) { return table; }
            table.Table.AddColumns("Key", "Value", "Type");
            response.ForEach(r => table.Table.AddRow(r.Key.EscapeMarkup(), LimitValue(r.Value), r.Type.EscapeMarkup()));
            return table;
        }

        private static string LimitValue(string? value, int limit = 100)
        {
            if (value == null) { return "[null]".EscapeMarkup(); }
            if (string.IsNullOrEmpty(value)) { return string.Empty; }

            value = SafeCliString(value);
            if (value.Length <= limit) { return value; }
            var chunk = value[0..(limit - 1)].Trim();
            return $"{chunk}\u2026";
        }

        private static string SerializeJobDetailsData(JobDetails? jobDetails)
        {
            if (jobDetails == null) { return string.Empty; }
            if (jobDetails.DataMap == null) { return string.Empty; }

            var result =
                jobDetails.DataMap.Count == 0 ?
                string.Empty :
                new Serializer().Serialize(jobDetails.DataMap);

            return result.Trim();
        }

        private static string SafeCliString(string? value, bool displayNull = false)
        {
            if (displayNull && value == null) { return "[null]".EscapeMarkup(); }
            if (string.IsNullOrWhiteSpace(value)) { return string.Empty; }
            return value.Trim().EscapeMarkup();
        }
    }
}