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
            table.AddRow(response.Id.ToString(), $"{response.Password}".EscapeMarkup());
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
                table.AddRow(item.Key.EscapeMarkup(), item.Value.EscapeMarkup());
            }

            return table;
        }

        public static Table GetTable(IEnumerable<LovItem>? response)
        {
            var table = new Table();
            if (response == null) { return table; }
            table.AddColumns("Id", "Name");
            foreach (var item in response)
            {
                table.AddRow(item.Id.ToString(), item?.Name.EscapeMarkup() ?? string.Empty);
            }

            return table;
        }

        public static Table GetTable(CliGeneralMarupMessageResponse? response)
        {
            var table = new Table();
            if (response == null) { return table; }
            if (response.MarkupMessages == null) { return table; }
            if (!response.MarkupMessages.Any()) { return table; }

            table.AddColumns(response.Title);
            foreach (var item in response.MarkupMessages)
            {
                table.AddRow(item);
            }

            return table;
        }

        public static Table GetTable(List<JobRowDetails>? response)
        {
            var table = new Table();
            if (response == null) { return table; }
            table.AddColumns("JobId", "Key (Group.Name)", "Description");
            response.ForEach(r => table.AddRow(r.Id, $"{r.Group}.{r.Name}".EscapeMarkup(), r.Description.EscapeMarkup()));
            return table;
        }

        public static Table GetTable(List<RunningJobDetails>? response)
        {
            var table = new Table();
            if (response == null) { return table; }
            table.AddColumns("FireInstanceId", "JobId", "Key (Group.Name)", "Progress", "EffectedRows", "RunTime");
            response.ForEach(r => table.AddRow(CliTableFormat.GetFireInstanceIdMarkup(r.FireInstanceId), $"{r.Id}", $"{r.Group}.{r.Name}".EscapeMarkup(), CliTableFormat.GetProgressMarkup(r.Progress), $"{r.EffectedRows}", CliTableFormat.FormatTimeSpan(r.RunTime)));
            return table;
        }

        public static Table GetTable(List<CliJobInstanceLog>? response)
        {
            var table = new Table();
            if (response == null) { return table; }
            table.AddColumns("Id", "JobId", "Key (Group.Name)", "TriggerId", "Status", "StartDate", "Duration", "EffectedRows");
            response.ForEach(r => table.AddRow($"{r.Id}", r.JobId, $"{r.JobGroup}.{r.JobName}".EscapeMarkup(), CliTableFormat.GetTriggerIdMarkup(r.TriggerId), CliTableFormat.GetStatusMarkup(r.Status), CliTableFormat.FormatDateTime(r.StartDate), CliTableFormat.FromatDuration(r.Duration), CliTableFormat.FormatNumber(r.EffectedRows)));
            return table;
        }

        public static Table GetTable(List<LogDetails>? response)
        {
            var table = new Table();
            if (response == null) { return table; }
            table.AddColumns("Id", "Message", "Level", "TimeStamp");
            response.ForEach(r => table.AddRow($"{r.Id}", r.Message.EscapeMarkup(), CliTableFormat.GetLevelMarkup(r.Level), CliTableFormat.FormatDateTime(r.TimeStamp)));
            return table;
        }

        public static Table GetTable(TriggerRowDetails? response)
        {
            var table = new Table();
            if (response == null) { return table; }
            table.AddColumns("TriggerId", "Key (Group.Name)", "State", "NextFireTime", "Interval/Cron");
            response.SimpleTriggers.ForEach(r => table.AddRow($"{r.Id}", $"{r.Group}.{r.Name}".EscapeMarkup(), r.State, CliTableFormat.FormatDateTime(r.NextFireTime), CliTableFormat.FormatTimeSpan(r.RepeatInterval)));
            response.CronTriggers.ForEach(r => table.AddRow($"{r.Id}", $"{r.Group}.{r.Name}".EscapeMarkup(), r.State, CliTableFormat.FormatDateTime(r.NextFireTime), r.CronExpression.EscapeMarkup()));
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
            table.AddColumns("Server", "Port", "InstanceId", "ClusterPort", "JoinDate", "HealthCheck");
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
            table.AddColumns("Id", "Name", "UserCount");
            data.ForEach(r => table.AddRow($"{r.Id}", r.Name.EscapeMarkup(), $"{r.UsersCount}"));
            return table;
        }

        public static Table GetTable(List<UserRowDetails>? data)
        {
            var table = new Table();
            if (data == null) { return table; }
            table.AddColumns("Id", "FirstName", "LastName", "Username", "EmailAddress1", "PhoneNumber1");
            data.ForEach(r => table.AddRow($"{r.Id}", r.FirstName.EscapeMarkup(), r.LastName.EscapeMarkup(), r.Username.EscapeMarkup(), r.EmailAddress1.EscapeMarkup(), r.PhoneNumber1.EscapeMarkup()));
            return table;
        }

        public static Table GetTable(List<PausedTriggerDetails>? response)
        {
            var table = new Table();
            if (response == null) { return table; }
            table.AddColumns("Trigger Id", "Key (Group.Name)", "Description", "Job Id");
            response.ForEach(r => table.AddRow(r.Id.EscapeMarkup(), $"{r.Group}.{r.Name}".EscapeMarkup(), r.Description.EscapeMarkup(), r.Id.EscapeMarkup()));
            return table;
        }

        internal static Table GetTable(List<CliGlobalConfig>? response)
        {
            var table = new Table();
            if (response == null) { return table; }
            table.AddColumns("Key", "Value", "Type");
            response.ForEach(r => table.AddRow(r.Key.EscapeMarkup(), r.Value.EscapeMarkup(), r.Type.EscapeMarkup()));
            return table;
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
    }
}