using Planar.CLI.Actions;
using Planar.CLI.Entities;
using Planar.CLI.Exceptions;
using Spectre.Console;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Planar.CLI
{
    public static class CliIterativeActions
    {
        public static async Task InvokeGetRunnings(CliGetRunningJobsRequest param)
        {
            var result = await JobCliActions.GetRunningJobsInner(param);
            var data = result.Item1;
            var response = result.Item2;

            if (response.IsSuccessful)
            {
                var ids = data.Select(r => r.Id).ToList();
                var table = CliTableExtensions.GetTable(result.Item1);
                await AnsiConsole.Live(table).StartAsync(async context =>
                {
                    context.Refresh();
                    if (ids.Count == 0) return;
                    await Task.Delay(2000);

                    var counter = 0;
                    while (counter < 1000)
                    {
                        var isAllFinish = await LoopGetRunnings(param, table, ids);
                        context.Refresh();

                        if (isAllFinish) { break; }
                        await Task.Delay(2000);
                        counter++;
                    }
                });
            }
        }

        private static async Task<bool> LoopGetRunnings(CliGetRunningJobsRequest param, Table table, List<string> ids)
        {
            var refreshResult = await JobCliActions.GetRunningJobsInner(param);
            var refreshData = refreshResult.Item1;
            var refreshResponse = refreshResult.Item2;

            if (refreshResponse.IsSuccessful == false)
            {
                throw new PlanarServiceException(null); // TODO: set restsharp response error text
            }

            for (int i = 0; i < table.Rows.Count; i++)
            {
                RefreshTable(table, ids, refreshData, i);
            }

            var isAllFinish = refreshData.All(r => r.Progress == 100);
            return isAllFinish;
        }

        private static void RefreshTable(Table table, List<string> ids, List<API.Common.Entities.RunningJobDetails> refreshData, int i)
        {
            var id = ids[i];
            var item = refreshData.FirstOrDefault(r => r.Id == id.ToString());
            if (item == null || item.Progress == 100)
            {
                table.UpdateCell(i, 3, $"[green]completed[/]");
                table.UpdateCell(i, 4, "---");
                table.UpdateCell(i, 5, "---");
            }
            else
            {
                table.UpdateCell(i, 3, $"[gold3_1]{item.Progress}%[/]");
                table.UpdateCell(i, 4, item.EffectedRows.ToString());
                table.UpdateCell(i, 5, item.RunTime);
            }
        }
    }
}