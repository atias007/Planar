using Planar.CLI.Actions;
using Planar.CLI.Entities;
using Planar.CLI.Exceptions;
using Spectre.Console;
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
            var response = result.Item2;

            if (response.IsSuccessful && data != null)
            {
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
                        var isAllFinish = await LoopGetRunnings(param, table.Table, ids, cancellationToken);
                        context.Refresh();

                        if (isAllFinish) { break; }
                        await Task.Delay(2000, cancellationToken);
                        counter++;
                    }
                });
            }
        }

        private static async Task<bool> LoopGetRunnings(CliGetRunningJobsRequest param, Table table, List<string> ids, CancellationToken cancellationToken)
        {
            var refreshResult = await JobCliActions.GetRunningJobsInner(param, cancellationToken);
            var refreshData = refreshResult.Item1;
            var refreshResponse = refreshResult.Item2;

            if (!refreshResponse.IsSuccessful)
            {
                var message = refreshResponse.ErrorMessage ?? "fail to get running jobs";
                throw new PlanarServiceException(message);
            }

            for (int i = 0; i < table.Rows.Count; i++)
            {
                RefreshTable(table, ids, refreshData, i);
            }

            var isAllFinish = refreshData?.TrueForAll(r => r.Progress == 100);
            return isAllFinish.GetValueOrDefault();
        }

        private static void RefreshTable(Table table, List<string> ids, List<API.Common.Entities.RunningJobDetails>? refreshData, int i)
        {
            if (refreshData == null) { return; }
            var id = ids[i];
            var item = refreshData.Find(r => r.Id == id.ToString());
            if (item == null || item.Progress == 100)
            {
                table.UpdateCell(i, 3, $"[grey]completed[/]");
                table.UpdateCell(i, 4, "---");
                table.UpdateCell(i, 5, "---");
                table.UpdateCell(i, 6, "[grey]---[/]");
            }
            else
            {
                var runtime = CliTableFormat.FormatTimeSpan(item.RunTime);
                var estimated = CliTableFormat.FormatTimeSpan(item.EstimatedEndTime);

                table.UpdateCell(i, 3, $"[gold3_1]{item.Progress}%[/]");
                table.UpdateCell(i, 4, item.EffectedRows.GetValueOrDefault().ToString());
                table.UpdateCell(i, 5, runtime);
                table.UpdateCell(i, 6, $"[grey]{estimated}[/]");
            }
        }
    }
}