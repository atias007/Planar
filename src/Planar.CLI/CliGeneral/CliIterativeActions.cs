﻿using Planar.CLI.Actions;
using Planar.CLI.Entities;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.CLI
{
    public static class CliIterativeActions
    {
        public static async Task InvokeGetRunnings(CliGetRunningJobsRequest param, CancellationToken cancellationToken)
        {
            var result = await JobCliActions.GetRunningJobsInner(param, cancellationToken);
            var data = result.Item1;

            var ids = data.Select(r => r.Id).ToList();
            var table = CliTableExtensions.GetTable(result.Item1);
            await AnsiConsole.Live(table.Table).StartAsync(async context =>
            {
                context.Refresh();
                if (ids.Count == 0) { return; }
                await Task.Delay(2000, cancellationToken);

                var counter = 0;
                while (counter < 1000)
                {
                    var isAllFinish = await RefreshTable(param, table.Table, ids, cancellationToken);
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
            });
        }

        private static async Task<bool> RefreshTable(CliGetRunningJobsRequest param, Table table, List<string> ids, CancellationToken cancellationToken)
        {
            var refreshResult = await JobCliActions.GetRunningJobsInner(param, cancellationToken);
            var refreshData = refreshResult.Item1;

            for (int i = 0; i < table.Rows.Count; i++)
            {
                RefreshTableRow(table, ids, refreshData, i);
            }

            var isAllFinish = refreshData?.TrueForAll(r => r.Progress == 100);
            return isAllFinish.GetValueOrDefault();
        }

        private static void RefreshTableRow(Table table, List<string> ids, List<API.Common.Entities.RunningJobDetails>? refreshData, int i)
        {
            if (refreshData == null) { return; }
            var id = ids[i];
            var item = refreshData.Find(r => r.Id == id.ToString());
            if (item == null || item.Progress == 100)
            {
                table.UpdateCell(i, 3, $"[grey]completed[/]");
                table.UpdateCell(i, 4, "---");
                table.UpdateCell(i, 5, "---");
                table.UpdateCell(i, 6, "---");
                table.UpdateCell(i, 7, "[grey]---[/]");
            }
            else
            {
                table.UpdateCell(i, 3, CliTableFormat.GetProgressMarkup(item.Progress));
                table.UpdateCell(i, 4, CliTableFormat.FormatNumber(item.EffectedRows));
                table.UpdateCell(i, 5, CliTableFormat.FormatExceptionCount(item.ExceptionsCount));
                table.UpdateCell(i, 6, CliTableFormat.FormatTimeSpan(item.RunTime));
                table.UpdateCell(i, 7, $"[grey]CliTableFormat.FormatTimeSpan(item.EstimatedEndTime)[/]");
            }
        }
    }
}