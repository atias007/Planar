using Planar.CLI.Actions;
using Planar.CLI.CliGeneral;
using Planar.CLI.Entities;
using Spectre.Console;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.CLI;

public static class CliIterativeActions
{
    public static async Task InvokeGetRunnings(CliGetRunningJobsRequest param, CancellationToken cancellationToken)
    {
        var result = await JobCliActions.GetRunningJobsInner(param, cancellationToken);
        var data = result.Item1;

        var table = CliTableExtensions.GetTable(data);
        table.DataSource = data.Select(i => i.FireInstanceId).ToList();
        await AnsiConsole.Live(table.Table).StartAsync(async context =>
        {
            context.Refresh();
            if (data.Count == 0) { return; }
            await Task.Delay(2000, cancellationToken);

            var counter = 0;
            while (counter < 1000)
            {
                var isAllFinish = await RefreshTable(param, table, cancellationToken);
                context.Refresh();

                if (isAllFinish) { break; }
                var delay = counter switch
                {
                    >= 0 and <= 60 => 1000,
                    > 60 and <= 180 => 2000,
                    > 180 and <= 500 => 4_000,
                    > 500 and <= 1000 => 10_000,
                    _ => 30_000
                };

                await Task.Delay(delay, cancellationToken);
                counter++;
            }

            SetAllCompleted(table.Table);
            context.Refresh();
        });
    }

    private static async Task<bool> RefreshTable(CliGetRunningJobsRequest param, CliTable table, CancellationToken cancellationToken)
    {
        var refreshResult = await JobCliActions.GetRunningJobsInner(param, cancellationToken);
        var newData = refreshResult.Item1;
        var existsData = table.DataSource as List<string>;
        if (newData == null || existsData == null) { return true; }

        // set completed rows
        for (int i = 0; i < existsData.Count; i++)
        {
            var fireInstanceId = existsData[i];
            var index = newData.FindIndex(r => r.FireInstanceId == fireInstanceId);
            if (index == -1)
            {
                UpdateCompletedTableRow(table.Table, i);
            }
        }

        // add & update existsing rows
        foreach (var item in newData)
        {
            var index = existsData.FindIndex(r => r == item.FireInstanceId);
            if (index == -1)
            {
                AddTableRow(table.Table, item);
                existsData.Add(item.FireInstanceId);
            }
            else
            {
                RefreshTableRow(table.Table, index, item);
            }
        }

        table.DataSource = existsData;
        var isAllFinish = newData.TrueForAll(r => r.Progress == 100);
        return isAllFinish;
    }

    private static void RefreshTableRow(Table table, int index, API.Common.Entities.RunningJobDetails data)
    {
        if (data == null || data.Progress == 100)
        {
            UpdateCompletedTableRow(table, index);
            return;
        }

        table.UpdateCell(index, 3, CliTableFormat.GetProgressMarkup(data.Progress));
        table.UpdateCell(index, 4, CliTableFormat.FormatNumber(data.EffectedRows));
        table.UpdateCell(index, 5, CliTableFormat.FormatExceptionCount(data.ExceptionsCount));
        table.UpdateCell(index, 6, CliTableFormat.FormatTimeSpan(data.RunTime));
        table.UpdateCell(index, 7, $"[grey]{CliTableFormat.FormatTimeSpan(data.EstimatedEndTime)}[/]");
    }

    private static void SetAllCompleted(Table table)
    {
        var rowsCount = table.Rows.Count;
        for (int i = 0; i < rowsCount; i++)
        {
            UpdateCompletedTableRow(table, i);
        }
    }

    private static void UpdateCompletedTableRow(Table table, int index)
    {
        if (index < 0)
        {
            Debugger.Break();
            return;
        }

        table.UpdateCell(index, 3, $"[grey]completed[/]");
        table.UpdateCell(index, 4, "---");
        table.UpdateCell(index, 5, "---");
        table.UpdateCell(index, 6, "---");
        table.UpdateCell(index, 7, "[grey]---[/]");
    }

    private static void AddTableRow(Table table, API.Common.Entities.RunningJobDetails data)
    {
        if (data == null) { return; }
        table.AddRow(
            CliTableFormat.GetFireInstanceIdMarkup(data.FireInstanceId),
            $"{data.Id}",
            CliTableFormat.FormatJobKey(data.Group, data.Name),
            CliTableFormat.GetProgressMarkup(data.Progress),
            CliTableFormat.FormatNumber(data.EffectedRows),
            CliTableFormat.FormatExceptionCount(data.ExceptionsCount),
            CliTableFormat.FormatTimeSpan(data.RunTime),
            $"[grey]{CliTableFormat.FormatTimeSpan(data.EstimatedEndTime)}[/]");
    }
}