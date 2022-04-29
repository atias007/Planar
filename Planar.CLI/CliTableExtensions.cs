using Planar.API.Common.Entities;
using Planar.CLI.Entities;
using RestSharp;
using Spectre.Console;
using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Planar.CLI
{
    public static class CliTableExtensions
    {
        public static Table GetTable(this GetAllJobsResponse response)
        {
            if (response.Success == false) return null;

            var table = new Table();
            table.AddColumns("JobId", "Key (Group.Name)", "Description");
            response.Result.ForEach(r => table.AddRow(r.Id, $"{r.Group}.{r.Name}".EscapeMarkup(), r.Description.EscapeMarkup()));
            return table;
        }

        public static Table GetTable(this GetRunningJobsResponse response)
        {
            if (response.Success == false) return null;

            var table = new Table();
            table.AddColumns("FireInstanceId", "JobId", "Key (Group.Name)", "Progress", "EffectedRows", "RunTime");
            response.Result.ForEach(r => table.AddRow(CliTableFormat.GetFireInstanceIdMarkup(r.FireInstanceId), $"{r.Id}", $"{r.Group}.{r.Name}".EscapeMarkup(), CliTableFormat.GetProgressMarkup(r.Progress), $"{r.EffectedRows}", CliTableFormat.FormatTimeSpan(r.RunTime)));
            return table;
        }

        public static Table GetTable(this List<JobInstanceLogRow> response)
        {
            var table = new Table();
            table.AddColumns("Id", "JobId", "Key (Group.Name)", "TriggerId", "Status", "StartDate", "Duration", "EffectedRows");
            response.ForEach(r => table.AddRow($"{r.Id}", r.JobId, $"{r.JobGroup}.{r.JobName}".EscapeMarkup(), CliTableFormat.GetTriggerIdMarkup(r.TriggerId), CliTableFormat.GetStatusMarkup(r.Status), CliTableFormat.FormatDateTime(r.StartDate), CliTableFormat.FromatDuration(r.Duration), CliTableFormat.FromatNumber(r.EffectedRows)));
            return table;
        }

        public static Table GetTable(this GetTraceResponse response)
        {
            if (response.Success == false) return null;

            var table = new Table();
            table.AddColumns("Id", "Message", "Level", "TimeStamp");
            response.Result.ForEach(r => table.AddRow($"{r.Id}", r.Message.EscapeMarkup(), CliTableFormat.GetLevelMarkup(r.Level), CliTableFormat.FormatDateTime(r.TimeStamp)));
            return table;
        }

        public static Table GetTable(BaseResponse<TriggerRowDetails> response)
        {
            if (response.Success == false) return null;
            var table = new Table();
            table.AddColumns("TriggerId", "Key (Group.Name)", "State", "NextFireTime", "Interval/Cron");
            response.Result.SimpleTriggers.ForEach(r => table.AddRow($"{r.Id}", $"{r.Group}.{r.Name}".EscapeMarkup(), r.State, CliTableFormat.FormatDateTime(r.NextFireTime), CliTableFormat.FormatTimeSpan(r.RepeatInterval)));
            response.Result.CronTriggers.ForEach(r => table.AddRow($"{r.Id}", $"{r.Group}.{r.Name}".EscapeMarkup(), r.State, CliTableFormat.FormatDateTime(r.NextFireTime), r.CronExpression.EscapeMarkup()));
            return table;
        }

        public static Table GetTable(BaseResponse response, List<UserRowDetails> data)
        {
            if (response.Success == false) return null;

            var table = new Table();
            table.AddColumns("Id", "FirstName", "LastName", "Username", "EmailAddress1", "PhoneNumber1");
            data.ForEach(r => table.AddRow($"{r.Id}", r.FirstName.EscapeMarkup(), r.LastName.EscapeMarkup(), r.Username.EscapeMarkup(), r.EmailAddress1.EscapeMarkup(), r.PhoneNumber1.EscapeMarkup()));
            return table;
        }

        public static Table GetTable(List<GroupRowDetails> data)
        {
            var table = new Table();
            table.AddColumns("Id", "Name");
            data.ForEach(r => table.AddRow($"{r.Id}", r.Name.EscapeMarkup()));
            return table;
        }

        public static List<Table> GetTable(BaseResponse<JobDetails> response)
        {
            if (response.Success == false) return null;
            var data = response.Result;
            var table = new Table();
            table.AddColumns("Property Name", "Value");
            table.AddRow(nameof(data.Id), data.Id.EscapeMarkup());
            table.AddRow(nameof(data.Group), data.Group.EscapeMarkup());
            table.AddRow(nameof(data.Name), data.Name.EscapeMarkup());
            table.AddRow(nameof(data.Description), data.Description.EscapeMarkup());
            table.AddRow(nameof(data.Durable), data.Durable.ToString());
            table.AddRow(nameof(data.RequestsRecovery), data.RequestsRecovery.ToString());
            table.AddRow(nameof(data.ConcurrentExecution), data.ConcurrentExecution.ToString());

            var properties =
                data.Properties?.Count == 0 ?
                null :
                new Serializer().Serialize(data.Properties);
            table.AddRow(nameof(data.Properties), properties.EscapeMarkup());

            var dataMap =
                data.DataMap?.Count == 0 ?
                null :
                new Serializer().Serialize(data.DataMap);
            table.AddRow(nameof(data.DataMap), dataMap.EscapeMarkup());

            var response2 = new BaseResponse<TriggerRowDetails> { Result = new TriggerRowDetails() };
            response2.Result.SimpleTriggers = data.SimpleTriggers;
            response2.Result.CronTriggers = data.CronTriggers;

            var table2 = GetTable(response2);

            return new List<Table> { table, table2 };
        }

        public static Table GetTable(BaseResponse<List<MonitorItem>> response)
        {
            if (response.Success == false) return null;
            var data = response.Result;
            var table = new Table();
            table.AddColumns("Id", "Title", "Event", "Job", "Group", "Hook", "Active");
            data.ForEach(r => table.AddRow(CliTableFormat.GetBooleanMarkup(r.Active, r.Id), r.Title.EscapeMarkup(), r.EventTitle.EscapeMarkup(), r.Job.EscapeMarkup(), r.GroupName.EscapeMarkup(), r.Hook.EscapeMarkup(), CliTableFormat.GetBooleanMarkup(r.Active)));
            return table;
        }

        public static Table GetTable<TKey>(Dictionary<TKey, string> dictionary, string titleColumnHeader)
        {
            var table = new Table();
            table.AddColumns("Id", titleColumnHeader);
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
            foreach (var item in list)
            {
                table.AddRow(Convert.ToString(item).EscapeMarkup());
            }

            return table;
        }
    }
}