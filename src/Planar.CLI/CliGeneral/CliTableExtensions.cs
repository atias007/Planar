using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Planar.API.Common.Entities;
using Planar.CLI.CliGeneral;
using Planar.CLI.Entities;
using Planar.Common;
using RestSharp;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using YamlDotNet.Serialization;

namespace Planar.CLI;

internal static class CliTableExtensions
{
    private static readonly string[] _forbiddenDataTableColumns = [
        nameof(JobHistory.Data),
        nameof(JobHistory.Log),
        nameof(JobHistory.Exception)
        ];

    public static CliTable GetCalendarsTable(IEnumerable<string>? items)
    {
        var table = new CliTable();
        if (items == null || !items.Any()) { return table; }

        var array = items.ToArray();
        const int columns = 5;
        var rows = Convert.ToInt32(Math.Ceiling(array.Length / (columns * 1.0)));

        int index = 0;
        string[,] matrix = new string[rows, columns];

        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < columns; y++)
            {
                matrix[x, y] = array[index];
                index++;
                if (index >= array.Length) { break; }
            }
        }

        table.Table.AddColumns(new int[columns * 2].Select(i => i.ToString()).ToArray());
        table.Table.HideHeaders();

        index = 0;
        for (int r = 0; r < rows; r++)
        {
            var rowItems = new string[columns * 2];
            for (int i = 0; i < columns; i++)
            {
                var value = matrix[r, i].EscapeMarkup() ?? string.Empty;
                var number = string.IsNullOrEmpty(value) ? string.Empty : (++index).ToString();
                rowItems[i * 2] = number;
                rowItems[i * 2 + 1] = value;
            }

            table.Table.AddRow(rowItems);
        }

        return table;
    }

    public static CliTable GetMetadataTable<T>() where T : class
    {
        var table = new CliTable();
        table.Table.AddColumns("Property Name", "Type");
        var properties = typeof(T).GetProperties();
        foreach (var p in properties)
        {
            var type = p.PropertyType.IsGenericType ? p.PropertyType.GenericTypeArguments[0].Name : p.PropertyType.Name;
            if (type == nameof(DateTime)) { type = nameof(DateTimeOffset); }
            table.Table.AddRow(p.Name, type);
        }

        return table;
    }

    public static CliTable GetTable(AddUserResponse? response)
    {
        var table = new CliTable();
        table.Table.AddColumns("Password");

        if (response == null) { return table; }
        table.Table.AddRow(SafeCliString(response.Password));
        table.Table.AddEmptyRow();
        table.Table.AddRow(CliFormat.GetWarningMarkup("make sure you copy the above password now."));
        table.Table.AddRow($"[{CliFormat.WarningColor}]we don't store it and you will not be able to see it again.[/]");
        return table;
    }

    public static CliTable GetTable(CliVersionData cliVersionData)
    {
        var table = new CliTable();
        table.Table.AddColumns("Service Version", CliTableFormat.FormatVersion(cliVersionData.ServiceVersion));
        table.Table.AddRow("CLI Version", CliTableFormat.FormatVersion(cliVersionData.CliVersion));
        return table;
    }

    public static CliTable GetTable(string key)
    {
        var table = new CliTable();
        table.Table.AddColumns("Cryptography Key");
        table.Table.AddRow(SafeCliString(key));
        table.Table.AddEmptyRow();
        table.Table.AddRow(CliFormat.GetWarningMarkup("make sure you copy the above cryptography key now."));
        table.Table.AddRow($"[{CliFormat.WarningColor}]set the key in server environment variable: PLANAR_CRYPTOGRAPHY_KEY[/]");
        table.Table.AddRow($"[{CliFormat.WarningColor}]we don't store it and you will not be able to see it again.[/]");
        return table;
    }

    public static CliTable GetTable(WorkingHoursModel? response)
    {
        var table = new CliTable();
        table.Table.AddColumns("Day Of Week", "Time Scope(s)");
        if (response == null) { return table; }

        foreach (var item in response.Days)
        {
            var text = CliTableFormat.GetTimeScopeString(item.Scopes);
            if (string.IsNullOrWhiteSpace(text))
            {
                text = $"[{CliFormat.ErrorColor}](no scopes)[/]";
            }

            table.Table.AddRow(item.DayOfWeek, text);
        }

        return table;
    }

    public static CliTable GetTable(JobMetrics? response)
    {
        var table = new CliTable();
        table.Table.AddColumns("Key", "Value");
        if (response == null) { return table; }

        table.Table.AddRow("Average Duration", CliTableFormat.FormatTimeSpan(response.AvgDuration));
        table.Table.AddRow("Standard Deviation Duration", CliTableFormat.FormatTimeSpan(response.StdevDuration));
        table.Table.AddRow("Average Effected Rows", response.AvgEffectedRows.ToString("N2"));
        table.Table.AddRow("Standard Deviation Effected Rows", response.StdevEffectedRows.ToString("N2"));
        table.Table.AddRow("Total Runs", response.TotalRuns.ToString("N0"));
        table.Table.AddRow("Success Retries", response.SuccessRetries.ToString("N0"));
        table.Table.AddRow("Fail Retries", response.FailRetries.ToString("N0"));
        table.Table.AddRow("Recovers", response.Recovers.ToString("N0"));
        return table;
    }

    public static CliTable GetTable(PagingResponse<JobAuditDto>? response, bool withJobId = false)
    {
        var table = new CliTable(paging: response, "audit");

        if (withJobId)
        {
            table.Table.AddColumns("Id", "Job Id", "Job Key", "Date Created", "Username", "User Title", "Description");
        }
        else
        {
            table.Table.AddColumns("Id", "Date Created", "Username", "User Title", "Description");
        }

        if (response == null || response.Data == null) { return table; }

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

    public static CliTable GetTable(IEnumerable<ReportsStatus>? response)
    {
        var table = new CliTable();
        table.Table.AddColumns("Period", "Enable", "Group", "Next Running");
        if (response == null) { return table; }

        foreach (var item in response)
        {
            if (item == null) { continue; }
            table.Table.AddRow(
                SafeCliString(item.Period),
                CliTableFormat.GetBooleanMarkup(item.Enabled),
                SafeCliString(item.Group),
                $"{CliTableFormat.FormatDate(item.NextRunning)} {CliTableFormat.FormatTime(item.NextRunning)}");
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

    public static CliTable GetTable(IEnumerable<MuteItem>? response)
    {
        var table = new CliTable(showCount: true);
        table.Table.AddColumns("Job Id", "Job Key", "Monitor Id", "Monitor Title", "Due Date");
        if (response == null) { return table; }

        foreach (var item in response)
        {
            if (item == null) { continue; }
            var jobid = item.JobId ?? $"[{CliFormat.WarningColor}][[all jobs]][/]";
            var monitorid = item.MonitorId?.ToString() ?? $"[{CliFormat.WarningColor}][[all monitors]][/]";
            table.Table.AddRow(jobid, CliTableFormat.FormatJobKey(item.JobGroup, item.JobName), monitorid, SafeCliString(item.MonitorTitle), CliTableFormat.FormatDateTime(item.DueDate));
        }

        return table;
    }

    public static CliTable GetTable(IEnumerable<HookInfo>? response)
    {
        var table = new CliTable(showCount: true);
        table.Table.AddColumns("Name", "Type", "Description");
        if (response == null) { return table; }

        foreach (var item in response)
        {
            if (item == null) { continue; }
            table.Table.AddRow(SafeCliString(item.Name), SafeCliString(item.HookType), SafeCliString(item.Description));
        }

        return table;
    }

    public static CliTable GetTable(MonitorHookDetails? response)
    {
        var table = new CliTable(showCount: true);
        if (response == null) { return table; }
        table.Table.AddColumns("Name", "Description");
        table.Table.AddRow(SafeCliString(response.Name), SafeCliString(response.Description));

        return table;
    }

    public static CliTable GetTable(IEnumerable<LovItem>? response, string entityName)
    {
        var table = new CliTable(showCount: true, entityName);
        table.Table.AddColumns("Id", "Name");
        if (response == null) { return table; }

        foreach (LovItem item in response)
        {
            table.Table.AddRow(item.Id.ToString(), LimitValue(item.Name));
        }

        return table;
    }

    public static CliTable GetTable(List<MonitorEventModel>? response)
    {
        var table = new CliTable(showCount: true, entityName: "event");
        table.Table.AddColumns("Event Title", "Event Type");

        if (response == null) { return table; }
        response.ForEach(r => table.Table.AddRow(r.EventTitle, r.EventType));
        return table;
    }

    public static CliTable GetTable(PagingResponse<JobBasicDetails>? response)
    {
        var table = new CliTable(paging: response, entityName: "job");
        table.Table.AddColumns("Job Id", "Job Key", "Job Type", "Description");
        if (response == null || response.Data == null) { return table; }
        var hasInactive = response.Data.Exists(d => d.Active != JobActiveMembers.Active);
        response.Data.ForEach(r => table.Table.AddRow(
            hasInactive ? CliTableFormat.FormatJobId(r.Id, r.Active) : r.Id,
            CliTableFormat.FormatJobKey(r.Group, r.Name),
            r.JobType.EscapeMarkup(),
            LimitValue(r.Description)));
        return table;
    }

    public static CliTable GetTable(List<RunningJobDetails>? response)
    {
        var table = new CliTable(showCount: true, "job");
        table.Table.AddColumns("Fire Instance Id", "Job Id", "Job Key", "Progress", "Effected Rows", "Ex. Count", "Run Time", "End Time");
        if (response == null) { return table; }

        response.ForEach(r => table.Table.AddRow(
            CliTableFormat.GetFireInstanceIdMarkup(r.FireInstanceId),
            $"{r.Id}",
            CliTableFormat.FormatJobKey(r.Group, r.Name),
            CliTableFormat.GetProgressMarkup(r.Progress),
            CliTableFormat.FormatNumber(r.EffectedRows),
            CliTableFormat.FormatExceptionCount(r.ExceptionsCount),
            CliTableFormat.FormatTimeSpan(r.RunTime),
            $"[grey]{CliTableFormat.FormatTimeSpan(r.EstimatedEndTime)}[/]"));
        return table;
    }

    public static CliTable GetTable(RestResponse? response)
    {
        var table = new CliTable(showCount: true);
        table.Table.AddColumn("no result from odata");

        if (response == null) { return table; }
        if (!response.IsSuccessStatusCode) { return table; }
        if (string.IsNullOrWhiteSpace(response.Content)) { return table; }
        var token = JToken.Parse(response.Content).SelectToken("$.value")?.ToString();
        if (string.IsNullOrWhiteSpace(token)) { return table; }
        dynamic? jsonObject = JsonConvert.DeserializeObject(token);
        if (jsonObject == null) { return table; }

        DataTable dataTable;
        try
        {
            dataTable = JsonConvert.DeserializeObject<DataTable>(Convert.ToString(jsonObject));
        }
        catch
        {
            return table;
        }

        if (dataTable == null) { return table; }

        // build columns
        var columns = dataTable.Columns.Cast<DataColumn>()
            .Select(c => c.ColumnName)
            .Distinct()
            .ToArray();

        ValidateDataTableColumns(columns);
        if (columns.Length > 0)
        {
            table = new CliTable(showCount: true);
            table.Table.AddColumns(columns);
        }

        // build rows
        for (var i = 0; i < dataTable.Rows.Count; i++)
        {
            var values = new List<string>();
            for (var j = 0; dataTable.Columns.Count > j; j++)
            {
                var value = GetValueForDataTable(dataTable.Rows[i][j], dataTable.Columns[j].ColumnName);
                values.Add(value);
            }

            table.Table.AddRow(values.ToArray());
        }

        return table;
    }

    public static CliTable GetTable(PagingResponse<JobInstanceLogRow>? response, bool singleJob = false)
    {
        var table = new CliTable(paging: response);

        if (singleJob)
        {
            table.Table.AddColumns("Id", "Trigger Id", "Status", "Start Date", "Duration", "Effected Rows");
            if (response == null || response.Data == null) { return table; }

            response.Data.ForEach(r => table.Table.AddRow(
                $"{r.Id}",
                CliTableFormat.GetTriggerIdMarkup(r.TriggerId ?? string.Empty),
                CliTableFormat.GetStatusMarkup(r.Status, r.HasWarnings),
                CliTableFormat.FormatDateTime(r.StartDate),
                CliTableFormat.FromatDuration(r.Duration),
                CliTableFormat.FormatNumber(r.EffectedRows)));
        }
        else
        {
            table.Table.AddColumns("Id", "Job Id", "Job Key", "Job Type", "Trigger Id", "Status", "Start Date", "Duration", "Effected Rows");
            if (response == null || response.Data == null) { return table; }

            response.Data.ForEach(r => table.Table.AddRow(
                $"{r.Id}",
                r.JobId ?? string.Empty,
                CliTableFormat.FormatJobKey(r.JobGroup, r.JobName),
                r.JobType.EscapeMarkup(),
                CliTableFormat.GetTriggerIdMarkup(r.TriggerId ?? string.Empty),
                CliTableFormat.GetStatusMarkup(r.Status, r.HasWarnings),
                CliTableFormat.FormatDateTime(r.StartDate),
                CliTableFormat.FromatDuration(r.Duration),
                CliTableFormat.FormatNumber(r.EffectedRows)));
        }

        return table;
    }

    public static CliTable GetTable(PagingResponse<JobLastRun>? response)
    {
        var table = new CliTable(paging: response);
        table.Table.AddColumns("Id", "Job Id", "Job Key", "Job Type", "Trigger Id", "Status", "Start Date", "Duration", "Effected Rows");
        if (response == null || response.Data == null) { return table; }

        response.Data.ForEach(r => table.Table.AddRow(
            $"{r.Id}",
            r.JobId ?? string.Empty,
            CliTableFormat.FormatJobKey(r.JobGroup, r.JobName),
            r.JobType.EscapeMarkup(),
            CliTableFormat.GetTriggerIdMarkup(r.TriggerId ?? string.Empty),
            CliTableFormat.GetStatusMarkup(r.Status, r.HasWarnings),
            CliTableFormat.FormatDateTime(r.StartDate),
            CliTableFormat.FromatDuration(r.Duration),
            CliTableFormat.FormatNumber(r.EffectedRows)));

        return table;
    }

    public static CliTable GetTable(PagingResponse<HistorySummary>? response)
    {
        var table = new CliTable(paging: response);
        table.Table.AddColumns("Job Id", "Job Key", "Total", "Success", "Fail", "Running", "Retries", "Effected Rows");
        if (response == null || response.Data == null) { return table; }

        response.Data.ForEach(r => table.Table.AddRow(
            r.JobId ?? string.Empty,
            CliTableFormat.FormatJobKey(r.JobGroup, r.JobName),
            CliTableFormat.FormatSummaryNumber(r.Total),
            CliTableFormat.FormatSummaryNumber(r.Success, CliFormat.OkColor),
            CliTableFormat.FormatSummaryNumber(r.Fail, CliFormat.ErrorColor),
            CliTableFormat.FormatSummaryNumber(r.Running, CliFormat.WarningColor),
            CliTableFormat.FormatSummaryNumber(r.Retries, "turquoise2"),
            CliTableFormat.FormatNumber(r.TotalEffectedRows)));

        return table;
    }

    public static CliTable GetTable(PagingResponse<ConcurrentExecutionModel>? response)
    {
        var table = new CliTable(paging: response);
        table.Table.AddColumns("Record Date", "Server", "InstanceId", "Max Concurrent");
        if (response == null || response.Data == null) { return table; }

        response.Data.ForEach(r => table.Table.AddRow(
            CliTableFormat.FormatDateTime(r.RecordDate),
            r.Server.EscapeMarkup(),
            r.InstanceId.EscapeMarkup(),
            CliTableFormat.FormatNumber(r.MaxConcurrent)));

        return table;
    }

    public static CliTable GetTable(PagingResponse<SecurityAuditModel>? response)
    {
        var table = new CliTable(paging: response);
        table.Table.AddColumns("Title", "Username", "User Title", "Date Created", "Is Warning");
        if (response == null || response.Data == null) { return table; }

        response.Data.ForEach(r => table.Table.AddRow(
            SafeCliString(r.Title),
            SafeCliString(r.Username),
            SafeCliString(r.UserTitle),
            CliTableFormat.FormatDateTime(r.DateCreated),
            CliTableFormat.GetBooleanWarningMarkup(r.IsWarning)));

        return table;
    }

    public static CliTable GetTable(PagingResponse<LogDetails>? response)
    {
        var table = new CliTable(paging: response);
        table.Table.AddColumns("Id", "Message", "Level", "Time Stamp");
        if (response == null || response.Data == null) { return table; }

        response.Data.ForEach(r => table.Table.AddRow(
            $"{r.Id}",
            SafeCliString(r.Message),
            CliTableFormat.GetLevelMarkup(r.Level),
            CliTableFormat.FormatDateTime(r.TimeStamp)));

        return table;
    }

    public static CliTable GetTable(JobCircuitBreaker? circuitBreaker)
    {
        var table = new CliTable();
        table.Table.AddColumns("Circuit Breaker", string.Empty);
        if (circuitBreaker == null) { return table; }

        table.Table.AddRow(nameof(circuitBreaker.FailureThreshold).SplitWords(), CliTableFormat.FormatNumber(circuitBreaker.FailureThreshold));
        table.Table.AddRow(nameof(circuitBreaker.FailCounter).SplitWords(), CliTableFormat.FormatNumber(circuitBreaker.FailCounter));
        table.Table.AddRow(nameof(circuitBreaker.SuccessThreshold).SplitWords(), CliTableFormat.FormatNumber(circuitBreaker.SuccessThreshold));
        table.Table.AddRow(nameof(circuitBreaker.SuccessCounter).SplitWords(), CliTableFormat.FormatNumber(circuitBreaker.SuccessCounter));
        table.Table.AddRow(nameof(circuitBreaker.PauseSpan).SplitWords(), CliTableFormat.FormatTimeSpan(circuitBreaker.PauseSpan));

        var activated = circuitBreaker.Activated ? $"[{CliFormat.ErrorColor}]Yes[/]" : $"[{CliFormat.OkColor}]No[/]";
        table.Table.AddRow(nameof(circuitBreaker.Activated), activated);

        table.Table.AddRow(nameof(circuitBreaker.ActivatedAt).SplitWords(), CliTableFormat.FormatDateTime(circuitBreaker.ActivatedAt));
        table.Table.AddRow(nameof(circuitBreaker.WillBeResetAt).SplitWords(), CliTableFormat.FormatDateTime(circuitBreaker.WillBeResetAt));

        return table;
    }

    public static CliTable GetTable(TriggerRowDetails? response)
    {
        var table = new CliTable(showCount: true, entityName: "trigger");
        table.Table.AddColumns("Trigger Id", "Trigger Name", "State", "Next Fire Time", "Interval/Cron");
        if (response == null) { return table; }

        var allActive = response.SimpleTriggers.TrueForAll(t => t.Active) && response.CronTriggers.TrueForAll(t => t.Active);

        response.SimpleTriggers.ForEach(r => table.Table.AddRow(
            allActive ? r.Id : CliTableFormat.FormatTriggerId(r.Id, r.Active),
            r.TriggerName.EscapeMarkup(),
            r.State ?? string.Empty,
            CliTableFormat.FormatDateTime(r.NextFireTime),
            CliTableFormat.FormatTimeSpan(r.RepeatInterval)));

        response.CronTriggers.ForEach(r => table.Table.AddRow(
            allActive ? r.Id : CliTableFormat.FormatTriggerId(r.Id, r.Active),
            r.TriggerName.EscapeMarkup(),
            r.State ?? string.Empty,
            CliTableFormat.FormatDateTime(r.NextFireTime),
            r.CronExpression.EscapeMarkup()));

        return table;
    }

    public static List<CliTable> GetTable(JobDetails? response)
    {
        var table = new CliTable();

        table.Table.AddColumns("Property Name", "Value");
        if (response == null)
        {
            return [table, table];
        }

        table.Table.AddRow(nameof(response.Id), response.Id.EscapeMarkup());
        table.Table.AddRow(nameof(response.Group), response.Group.EscapeMarkup());
        table.Table.AddRow(nameof(response.Name), response.Name.EscapeMarkup());
        table.Table.AddRow(nameof(response.Author), response.Author.EscapeMarkup());
        table.Table.AddRow(nameof(response.LogRetentionDays).SplitWords(), Convert.ToString(response.LogRetentionDays) ?? string.Empty);
        table.Table.AddRow(nameof(response.JobType).SplitWords(), response.JobType.EscapeMarkup());
        table.Table.AddRow(nameof(response.Description), response.Description.EscapeMarkup());
        table.Table.AddRow(nameof(response.Durable), response.Durable.ToString());
        table.Table.AddRow(nameof(response.RequestsRecovery).SplitWords(), response.RequestsRecovery.ToString());
        table.Table.AddRow(nameof(response.Concurrent), response.Concurrent.ToString());
        table.Table.AddRow(nameof(response.CircuitBreaker).SplitWords(), GetCircuitBreakerStatusLabel(response));
        table.Table.AddRow(nameof(response.Active), CliTableFormat.FormatActive(response.Active));

        if (response.AutoResume.HasValue)
        {
            var text = CliTableFormat.FormatDateTime(response.AutoResume.Value);
            var colorText = $"[{CliFormat.OkColor}]{text}[/]";
            table.Table.AddRow(nameof(response.AutoResume), colorText);
        }

        var dataMap = SerializeJobDetailsData(response);

        table.Table.AddRow("Data", dataMap.EscapeMarkup() ?? string.Empty);
        table.Table.AddRow(nameof(response.Properties), response.Properties.EscapeMarkup());

        var response2 = new TriggerRowDetails
        {
            SimpleTriggers = response.SimpleTriggers,
            CronTriggers = response.CronTriggers
        };

        var table2 = GetTable(response2);

        return [table, table2];
    }

    public static CliTable GetTable(List<CliClusterNode>? response)
    {
        var table = new CliTable(showCount: true, entityName: "node");
        table.Table.AddColumns("Server", "Port", "Instance Id", "Cluster Port", "Join Date", "Health Check");
        if (response == null) { return table; }

        response.ForEach(r =>
        {
            var hcTitle = CliTableFormat.FormatClusterHealthCheck(r.HealthCheckGap, r.HealthCheckGapDeviation);
            table.Table.AddRow(
                r.Server.EscapeMarkup(),
                r.Port.ToString(),
                r.InstanceId.EscapeMarkup(),
                r.ClusterPort.ToString(),
                CliTableFormat.FormatDateTime(r.JoinDate), hcTitle);
        });

        return table;
    }

    public static CliTable GetTable(PagingResponse<MonitorItem>? response)
    {
        var table = new CliTable(paging: response, entityName: "monitor");

        table.Table.AddColumns("Id", "Title", "Event", "Job Group", "Job Name", "Event Argument", "Dist. Groups", "Hook", "Active");
        if (response == null || response.Data == null) { return table; }

        response.Data.ForEach(r => table.Table.AddRow(
            r.Id.ToString(),
            r.Title.EscapeMarkup(),
            r.Event.EscapeMarkup(),
            r.JobGroup.EscapeMarkup(),
            r.JobName.EscapeMarkup(),
            r.EventArgument.EscapeMarkup(),
            string.Join(',', r.DistributionGroups),
            r.Hook.EscapeMarkup(),
            CliTableFormat.GetBooleanMarkup(r.Active)));

        return table;
    }

    public static CliTable GetTable(PagingResponse<MonitorAlertRowModel>? response)
    {
        var table = new CliTable(paging: response, entityName: "alert");

        table.Table.AddColumns("Id", "Monitor Title", "Event Title", "Event Arguments", "Alert Date", "JobId", "Job Key", "Dist. Group", "Hook", "Has Error");
        if (response == null || response.Data == null) { return table; }

        response.Data.ForEach(r => table.Table.AddRow(
            r.Id.ToString(),
            SafeCliString(r.MonitorTitle),
            r.EventTitle.EscapeMarkup(),
            r.EventArgument.EscapeMarkup(),
            CliTableFormat.FormatDateTime(r.AlertDate),
            r.JobId.EscapeMarkup(),
            CliTableFormat.FormatJobKey(r.JobGroup, r.JobName),
            SafeCliString(r.GroupName),
            r.Hook.EscapeMarkup(),
            r.HasError.ToString()));

        return table;
    }

    public static CliTable GetTable(PagingResponse<GroupInfo>? response)
    {
        var table = new CliTable(paging: response, entityName: "group");
        table.Table.AddColumns("Name", "Role", "User Count");
        if (response == null || response.Data == null) { return table; }

        response.Data.ForEach(r => table.Table.AddRow(r.Name.EscapeMarkup(), r.Role.EscapeMarkup(), $"{r.UsersCount}"));
        return table;
    }

    public static CliTable GetTable(PagingResponse<UserRowModel>? data)
    {
        var table = new CliTable(paging: data, entityName: "user");
        table.Table.AddColumns("Username", "First Name", "Last Name", "Email Address 1", "Phone Number 1");
        if (data == null || data.Data == null) { return table; }

        data.Data.ForEach(r => table.Table.AddRow(r.Username.EscapeMarkup(), r.FirstName.EscapeMarkup(), r.LastName.EscapeMarkup(), r.EmailAddress1.EscapeMarkup(), r.PhoneNumber1.EscapeMarkup()));
        return table;
    }

    public static CliTable GetTable(List<PausedTriggerDetails>? response)
    {
        var table = new CliTable(showCount: true, entityName: "trigger");
        table.Table.AddColumns("Trigger Id", "Trigger Name", "Job Id", "Job Key");
        if (response == null) { return table; }

        response.ForEach(r => table.Table.AddRow(r.Id.EscapeMarkup(), r.TriggerName.EscapeMarkup(), r.JobId.EscapeMarkup(), CliTableFormat.FormatJobKey(r.JobGroup, r.JobName)));
        return table;
    }

    internal static CliTable GetTable(List<CliGlobalConfig>? response)
    {
        var table = new CliTable(showCount: true);
        table.Table.AddColumns("Key", "Value", "Type", "Source Url", "Last Update");

        if (response == null) { return table; }
        response.ForEach(r => table.Table.AddRow(
            r.Key.EscapeMarkup(),
            SafeCliString(LimitValue(r.Value)),
            r.Type.EscapeMarkup(),
            SafeCliString(LimitValue(r.SourceUrl)),
            CliTableFormat.FormatDateTime(r.LastUpdate)));

        return table;
    }

    private static string GetCircuitBreakerStatusLabel(JobDetails response)
    {
        if (response.CircuitBreaker == null)
        {
            return "Disabled";
        }

        return response.CircuitBreaker.Activated ? $"[{CliFormat.ErrorColor}]Activated[/]" : "Enabled";
    }

    private static string GetValueForDataTable(object? value, string columnName)
    {
        if (value == null) { return string.Empty; }
        if (value.Equals(DBNull.Value)) { return string.Empty; }
        if (columnName.Equals(nameof(JobHistory.StartDate), StringComparison.OrdinalIgnoreCase))
        {
            return CliTableFormat.FormatDateTime(Convert.ToDateTime(value));
        }

        if (columnName.Equals(nameof(JobHistory.EndDate), StringComparison.OrdinalIgnoreCase))
        {
            return CliTableFormat.FormatDateTime(Convert.ToDateTime(value));
        }

        if (columnName.Equals(nameof(JobHistory.Duration), StringComparison.OrdinalIgnoreCase))
        {
            return CliTableFormat.FromatDuration(Convert.ToInt32(value));
        }

        if (columnName.Equals(nameof(JobHistory.EffectedRows), StringComparison.OrdinalIgnoreCase))
        {
            return CliTableFormat.FormatNumber(Convert.ToInt32(value));
        }

        if (columnName.Equals(nameof(JobHistory.ExceptionCount), StringComparison.OrdinalIgnoreCase))
        {
            return CliTableFormat.FormatExceptionCount(Convert.ToInt32(value));
        }

        if (columnName.Equals(nameof(JobHistory.StatusTitle), StringComparison.OrdinalIgnoreCase))
        {
            return CliTableFormat.GetStatusMarkup(Convert.ToString(value));
        }

        if (columnName.Equals(nameof(JobHistory.TriggerId), StringComparison.OrdinalIgnoreCase))
        {
            return CliTableFormat.GetTriggerIdMarkup(Convert.ToString(value));
        }

        return SafeCliString(value.ToString(), displayNull: true);
    }

    private static string LimitValue(string? value, int limit = 100, bool displayNull = false)
    {
        if (displayNull && value == null) { return "[null]".EscapeMarkup(); }
        if (string.IsNullOrEmpty(value)) { return string.Empty; }

        value = SafeCliString(value);
        if (value.Length <= limit) { return value; }
        var chunk = value[0..(limit - 1)].Trim();
        return $"{chunk}\u2026";
    }

    private static string SafeCliString(string? value, bool displayNull = false)
    {
        const string tab = "    ";
        if (displayNull && value == null) { return "[null]".EscapeMarkup(); }
        if (string.IsNullOrWhiteSpace(value)) { return string.Empty; }
        value = value.Replace("\t", tab);
        return value.Trim().EscapeMarkup();
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

    private static void ValidateDataTableColumns(IEnumerable<string> columns)
    {
        // columns count
        const int maxColumns = 10;
        if (columns.Count() > maxColumns)
        {
            throw new CliWarningException($"there are more then maximum allowed columns ({maxColumns}) to display in CLI");
        }

        // forbidden columns
        if (columns.Any(c => _forbiddenDataTableColumns.Contains(c)))
        {
            var forbiddenColumns = columns
                .Where(c => _forbiddenDataTableColumns.Contains(c))
                .Select(c => c.ToLower())
                .ToArray();

            var message = forbiddenColumns.Length == 1 ?
                $"column {string.Join(", ", forbiddenColumns)} is not allowed to be display in CLI" :
                $"columns {string.Join(", ", forbiddenColumns)} are not allowed to be display in CLI";

            throw new CliWarningException(message);
        }
    }
}