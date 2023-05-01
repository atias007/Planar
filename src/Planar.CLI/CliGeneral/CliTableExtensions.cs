using Planar.API.Common.Entities;
using Planar.CLI.CliGeneral;
using Planar.CLI.Entities;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Serialization;

namespace Planar.CLI
{
    internal static class CliTableExtensions
    {
        public static Table GetTable(AddUserResponse? response)
        {
            var table = new Table();
            if (response == null) { return table; }
            table.AddColumns("User Id", "Password");
            table.AddRow(response.Id.ToString(), SafeCliString(response.Password));
            table.AddRow(string.Empty, string.Empty);
            table.AddRow(string.Empty, CliFormat.GetWarningMarkup("make sure you copy the above password now."));
            table.AddRow(string.Empty, $"[{CliFormat.WarningColor}]we don't store it and you will not be able to see it again.[/]");
            return table;
        }

        public static Table GetTable(IEnumerable<KeyValueItem>? response)
        {
            var table = new Table();
            if (response == null) { return table; }
            table.AddColumns("Key", "Value");
            foreach (var item in response)
            {
                if (item == null) { continue; }
                table.AddRow(SafeCliString(item.Key), LimitValue(item.Value));
            }

            return table;
        }

        public static Table GetTable(IEnumerable<LovItem>? response)
        {
            var table = new Table();
            if (response == null) { return table; }
            table.AddColumns("Id", "Name");
            foreach (LovItem item in response)
            {
                table.AddRow(item.Id.ToString(), LimitValue(item.Name));
            }

            return table;
        }

        public static Table GetTable(List<JobRowDetails>? response)
        {
            var table = new Table();
            if (response == null) { return table; }
            table.AddColumns("Job Id", "Job Key", "Job Type", "Description");
            response.ForEach(r => table.AddRow(r.Id, $"{r.Group}.{r.Name}".EscapeMarkup(), r.JobType.EscapeMarkup(), LimitValue(r.Description)));
            return table;
        }

        public static Table GetTable(List<RunningJobDetails>? response)
        {
            var table = new Table();
            if (response == null) { return table; }
            table.AddColumns("Fire Instance Id", "Job Id", "Job Key", "Progress", "Effected Rows", "Run Time", "End Time");
            response.ForEach(r => table.AddRow(
                CliTableFormat.GetFireInstanceIdMarkup(r.FireInstanceId),
                $"{r.Id}",
                $"{r.Group}.{r.Name}".EscapeMarkup(),
                CliTableFormat.GetProgressMarkup(r.Progress),
                $"{r.EffectedRows}",
                CliTableFormat.FormatTimeSpan(r.RunTime),
                $"[grey]{CliTableFormat.FormatTimeSpan(r.EstimatedEndTime)}[/]"));
            return table;
        }

        public static Table GetTable(List<CliJobInstanceLog>? response)
        {
            var table = new Table();
            if (response == null) { return table; }
            table.AddColumns("Id", "Job Id", "Job Key", "Job Type", "Trigger Id", "Status", "Start Date", "Duration", "Effected Rows");
            response.ForEach(r => table.AddRow($"{r.Id}", r.JobId, $"{r.JobGroup}.{r.JobName}".EscapeMarkup(), r.JobType.EscapeMarkup(), CliTableFormat.GetTriggerIdMarkup(r.TriggerId), CliTableFormat.GetStatusMarkup(r.Status), CliTableFormat.FormatDateTime(r.StartDate), CliTableFormat.FromatDuration(r.Duration), CliTableFormat.FormatNumber(r.EffectedRows)));
            return table;
        }

        public static Table GetTable(List<LogDetails>? response)
        {
            var table = new Table();
            if (response == null) { return table; }
            table.AddColumns("Id", "Message", "Level", "Time Stamp");
            response.ForEach(r => table.AddRow($"{r.Id}", LimitValue(r.Message), CliTableFormat.GetLevelMarkup(r.Level), CliTableFormat.FormatDateTime(r.TimeStamp)));
            return table;
        }

        public static Table GetTable(TriggerRowDetails? response)
        {
            var table = new Table();
            if (response == null) { return table; }
            table.AddColumns("Trigger Id", "Trigger Key", "State", "Next Fire Time", "Interval/Cron");
            response.SimpleTriggers.ForEach(r => table.AddRow($"{r.Id}", $"{r.TriggerGroup}.{r.TriggerName}".EscapeMarkup(), r.State ?? string.Empty, CliTableFormat.FormatDateTime(r.NextFireTime), CliTableFormat.FormatTimeSpan(r.RepeatInterval)));
            response.CronTriggers.ForEach(r => table.AddRow($"{r.Id}", $"{r.TriggerGroup}.{r.TriggerName}".EscapeMarkup(), r.State ?? string.Empty, CliTableFormat.FormatDateTime(r.NextFireTime), r.CronExpression.EscapeMarkup()));
            return table;
        }

        public static List<Table> GetTable(JobDetails? response)
        {
            var table = new Table();
            if (response == null)
            {
                return new List<Table> { table };
            }

            table.AddColumns("Property Name", "Value");
            table.AddRow(nameof(response.Id), response.Id.EscapeMarkup());
            table.AddRow(nameof(response.Group), response.Group.EscapeMarkup());
            table.AddRow(nameof(response.Name), response.Name.EscapeMarkup());
            table.AddRow(nameof(response.Author), response.Author.EscapeMarkup());
            table.AddRow(nameof(response.JobType), response.JobType.EscapeMarkup());
            table.AddRow(nameof(response.Description), response.Description.EscapeMarkup());
            table.AddRow(nameof(response.Durable), response.Durable.ToString());
            table.AddRow(nameof(response.RequestsRecovery), response.RequestsRecovery.ToString());
            table.AddRow(nameof(response.Concurrent), response.Concurrent.ToString());

            var dataMap = SerializeJobDetailsData(response);

            table.AddRow("Data", dataMap.EscapeMarkup() ?? string.Empty);
            table.AddRow(nameof(response.Properties), response.Properties.EscapeMarkup());

            var response2 = new TriggerRowDetails
            {
                SimpleTriggers = response.SimpleTriggers,
                CronTriggers = response.CronTriggers
            };

            var table2 = GetTable(response2);

            return new List<Table> { table, table2 };
        }

        public static Table GetTable(List<CliClusterNode>? response)
        {
            var table = new Table();
            if (response == null) { return table; }
            table.AddColumns("Server", "Port", "Instance Id", "Cluster Port", "Join Date", "Health Check");
            response.ForEach(r =>
            {
                var hcTitle = CliTableFormat.FormatClusterHealthCheck(r.HealthCheckGap, r.HealthCheckGapDeviation);
                table.AddRow(r.Server.EscapeMarkup(), r.Port.ToString(), r.InstanceId.EscapeMarkup(), r.ClusterPort.ToString(), CliTableFormat.FormatDateTime(r.JoinDate), hcTitle);
            });

            return table;
        }

        public static Table GetTable(List<MonitorItem>? response)
        {
            var table = new Table();
            if (response == null) { return table; }
            var data = response;
            table.AddColumns("Id", "Title", "Event", "Job Group", "Job Name", "Dist. Group", "Hook", "Active");
            data.ForEach(r => table.AddRow(
                r.Id.ToString(),
                r.Title.EscapeMarkup(),
                r.EventTitle.EscapeMarkup(),
                r.JobGroup.EscapeMarkup(),
                r.JobName.EscapeMarkup(),
                r.DistributionGroupName.EscapeMarkup(),
                r.Hook.EscapeMarkup(),
                CliTableFormat.GetBooleanMarkup(r.Active)));
            return table;
        }

        public static Table GetTable(List<GroupInfo>? data)
        {
            var table = new Table();
            if (data == null) { return table; }
            table.AddColumns("Id", "Name", "Role", "User Count");
            data.ForEach(r => table.AddRow($"{r.Id}", r.Name.EscapeMarkup(), r.Role.EscapeMarkup(), $"{r.UsersCount}"));
            return table;
        }

        public static Table GetTable(List<UserRowDetails>? data)
        {
            var table = new Table();
            if (data == null) { return table; }
            table.AddColumns("Id", "First Name", "Last Name", "Username", "Email Address 1", "Phone Number 1");
            data.ForEach(r => table.AddRow($"{r.Id}", r.FirstName.EscapeMarkup(), r.LastName.EscapeMarkup(), r.Username.EscapeMarkup(), r.EmailAddress1.EscapeMarkup(), r.PhoneNumber1.EscapeMarkup()));
            return table;
        }

        public static Table GetTable(List<PausedTriggerDetails>? response)
        {
            var table = new Table();
            if (response == null) { return table; }
            table.AddColumns("Trigger Id", "Trigger Key", "Job Id", "Job Key");
            response.ForEach(r => table.AddRow(r.Id.EscapeMarkup(), $"{r.TriggerGroup}.{r.TriggerName}".EscapeMarkup(), r.JobId.EscapeMarkup(), $"{r.JobGroup}.{r.JobName}".EscapeMarkup()));
            return table;
        }

        internal static Table GetTable(List<CliGlobalConfig>? response)
        {
            var table = new Table();
            if (response == null) { return table; }
            table.AddColumns("Key", "Value", "Type");
            response.ForEach(r => table.AddRow(r.Key.EscapeMarkup(), LimitValue(r.Value), r.Type.EscapeMarkup()));
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