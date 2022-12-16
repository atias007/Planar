using Planar.API.Common.Entities;
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
        public static Table GetTable(AddUserResponse response)
        {
            var table = new Table();
            if (response == null) { return table; }
            table.AddColumns("User Id", "Password");
            table.AddRow(response.Id.ToString(), $"{response.Password}".EscapeMarkup());
            table.AddRow(string.Empty, string.Empty);
            table.AddRow(string.Empty, $"{CliTableFormat.WarningColor} Warning! Make sure you copy the above password now.[/]");
            table.AddRow(string.Empty, $"{CliTableFormat.WarningColor} We don't store it and you will not be able to see it again.[/]");
            return table;
        }

        public static Table GetTable(IEnumerable<KeyValueItem> response)
        {
            var table = new Table();
            if (response == null) { return table; }
            table.AddColumns("Key", "Value");
            foreach (var item in response)
            {
                table.AddRow(item?.Key.EscapeMarkup(), item?.Value.EscapeMarkup());
            }

            return table;
        }

        public static Table GetTable(CliGeneralMarupMessageResponse response)
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

        public static Table GetTable(List<JobRowDetails> response)
        {
            var table = new Table();
            if (response == null) { return table; }
            table.AddColumns("JobId", "Key (Group.Name)", "Description");
            response.ForEach(r => table.AddRow(r.Id, $"{r.Group}.{r.Name}".EscapeMarkup(), r.Description.EscapeMarkup()));
            return table;
        }

        public static Table GetTable(List<RunningJobDetails> response)
        {
            var table = new Table();
            if (response == null) { return table; }
            table.AddColumns("FireInstanceId", "JobId", "Key (Group.Name)", "Progress", "EffectedRows", "RunTime");
            response.ForEach(r => table.AddRow(CliTableFormat.GetFireInstanceIdMarkup(r.FireInstanceId), $"{r.Id}", $"{r.Group}.{r.Name}".EscapeMarkup(), CliTableFormat.GetProgressMarkup(r.Progress), $"{r.EffectedRows}", CliTableFormat.FormatTimeSpan(r.RunTime)));
            return table;
        }

        public static Table GetTable(List<CliJobInstanceLog> response)
        {
            var table = new Table();
            if (response == null) { return table; }
            table.AddColumns("Id", "JobId", "Key (Group.Name)", "TriggerId", "Status", "StartDate", "Duration", "EffectedRows");
            response.ForEach(r => table.AddRow($"{r.Id}", r.JobId, $"{r.JobGroup}.{r.JobName}".EscapeMarkup(), CliTableFormat.GetTriggerIdMarkup(r.TriggerId), CliTableFormat.GetStatusMarkup(r.Status), CliTableFormat.FormatDateTime(r.StartDate), CliTableFormat.FromatDuration(r.Duration), CliTableFormat.FromatNumber(r.EffectedRows)));
            return table;
        }

        public static Table GetTable(List<LogDetails> response)
        {
            var table = new Table();
            table.AddColumns("Id", "Message", "Level", "TimeStamp");
            response.ForEach(r => table.AddRow($"{r.Id}", r.Message.EscapeMarkup(), CliTableFormat.GetLevelMarkup(r.Level), CliTableFormat.FormatDateTime(r.TimeStamp)));
            return table;
        }

        internal static Table GetTable(List<CliGlobalConfig> response)
        {
            var table = new Table();
            table.AddColumns("Key", "Value", "Type");
            response.ForEach(r => table.AddRow(r.Key.EscapeMarkup(), r.Value.EscapeMarkup(), r.Type.EscapeMarkup()));
            return table;
        }

        public static Table GetTable(TriggerRowDetails response)
        {
            var table = new Table();
            table.AddColumns("TriggerId", "Key (Group.Name)", "State", "NextFireTime", "Interval/Cron");
            response.SimpleTriggers.ForEach(r => table.AddRow($"{r.Id}", $"{r.Group}.{r.Name}".EscapeMarkup(), r.State, CliTableFormat.FormatDateTime(r.NextFireTime), r.RepeatInterval));
            response.CronTriggers.ForEach(r => table.AddRow($"{r.Id}", $"{r.Group}.{r.Name}".EscapeMarkup(), r.State, CliTableFormat.FormatDateTime(r.NextFireTime), r.CronExpression.EscapeMarkup()));
            return table;
        }

        public static Table GetTable(List<UserRowDetails> data)
        {
            var table = new Table();
            table.AddColumns("Id", "FirstName", "LastName", "Username", "EmailAddress1", "PhoneNumber1");
            data.ForEach(r => table.AddRow($"{r.Id}", r.FirstName.EscapeMarkup(), r.LastName.EscapeMarkup(), r.Username.EscapeMarkup(), r.EmailAddress1.EscapeMarkup(), r.PhoneNumber1.EscapeMarkup()));
            return table;
        }

        public static Table GetTable(List<GroupInfo> data)
        {
            var table = new Table();
            if (data == null) { return table; }
            table.AddColumns("Id", "Name", "UserCount");
            data.ForEach(r => table.AddRow($"{r.Id}", r.Name.EscapeMarkup(), $"{r.UsersCount}"));
            return table;
        }

        public static List<Table> GetTable(JobDetails response)
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
            table.AddRow(nameof(response.Description), response.Description.EscapeMarkup());
            table.AddRow(nameof(response.Durable), response.Durable.ToString());
            table.AddRow(nameof(response.RequestsRecovery), response.RequestsRecovery.ToString());
            table.AddRow(nameof(response.Concurrent), response.Concurrent.ToString());

            var dataMap =
                response.DataMap?.Count == 0 ?
                string.Empty :
                new Serializer().Serialize(response.DataMap);

            table.AddRow("Data", dataMap?.Trim().EscapeMarkup());
            table.AddRow(nameof(response.Properties), response.Properties.EscapeMarkup());

            var response2 = new TriggerRowDetails
            {
                SimpleTriggers = response.SimpleTriggers,
                CronTriggers = response.CronTriggers
            };

            var table2 = GetTable(response2);

            return new List<Table> { table, table2 };
        }

        public static Table GetTable(List<MonitorItem> response)
        {
            if (response == null) { return null; }
            var data = response;
            var table = new Table();
            table.AddColumns("Id", "Title", "Event", "Job", "Group", "Hook", "Active");
            data.ForEach(r => table.AddRow(CliTableFormat.GetBooleanMarkup(r.Active, r.Id), r.Title.EscapeMarkup(), r.EventTitle.EscapeMarkup(), r.Job.EscapeMarkup(), r.GroupName.EscapeMarkup(), r.Hook.EscapeMarkup(), CliTableFormat.GetBooleanMarkup(r.Active)));
            return table;
        }

        public static Table GetTable(List<CliClusterNode> response)
        {
            if (response == null) { return null; }
            var table = new Table();
            table.AddColumns("Server", "Port", "InstanceId", "ClusterPort", "JoinDate", "HealthCheck");
            response.ForEach(r =>
            {
                var hcTitle = CliTableFormat.FormatClusterHealthCheck(r.HealthCheckGap, r.HealthCheckGapDeviation);
                table.AddRow(r.Server.EscapeMarkup(), r.Port.ToString(), r.InstanceId.EscapeMarkup(), r.ClusterPort.ToString(), CliTableFormat.FormatDateTime(r.JoinDate), hcTitle);
            });

            return table;
        }

        public static Table GetTable<TKey>(Dictionary<TKey, string> dictionary, string titleColumnHeader)
        {
            var table = new Table();
            table.AddColumns("Id", titleColumnHeader);
            if (dictionary == null) { return table; }
            foreach (var item in dictionary)
            {
                table.AddRow(Convert.ToString(item.Key).EscapeMarkup(), item.Value.EscapeMarkup());
            }

            return table;
        }

        public static Table GetTable<T>(List<T> list, string columnHeader)
        {
            var table = new Table();
            table.AddColumns(columnHeader);
            if (list == null) { return table; }
            foreach (var item in list)
            {
                table.AddRow(Convert.ToString(item).EscapeMarkup());
            }

            return table;
        }
    }
}