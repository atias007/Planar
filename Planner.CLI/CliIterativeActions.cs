using Planner.API.Common.Entities;
using Planner.CLI.Actions;
using Planner.CLI.Entities;
using Planner.CLI.Exceptions;
using Spectre.Console;
using System.Linq;
using System.Threading.Tasks;

namespace Planner.CLI
{
    public static class CliIterativeActions
    {
        public static async Task InvokeGetRunnings(CliGetRunningJobsRequest param)
        {
            var result = await JobCliActions.GetRunningJobs(param);
            if (result.Response.Success)
            {
                var ids = (result.Response as GetRunningJobsResponse).Result.Select(r => r.Id).ToList();
                var table = result.Tables.First();
                await AnsiConsole.Live(table).StartAsync(async ctx =>
                {
                    ctx.Refresh();
                    if (ids.Count == 0) return;
                    await Task.Delay(2000);

                    var counter = 0;
                    while (counter < 1000)
                    {
                        var refreshResult = await JobCliActions.GetRunningJobs(param);

                        if (refreshResult.Response.Success == false)
                        {
                            throw new PlannerServiceException(refreshResult.Response);
                        }

                        var respone = refreshResult.Response as GetRunningJobsResponse;
                        for (int i = 0; i < table.Rows.Count; i++)
                        {
                            var id = ids[i];
                            var item = respone.Result.FirstOrDefault(r => r.Id == id.ToString());
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
                                table.UpdateCell(i, 5, CliTableFormat.FormatTimeSpan(item.RunTime));
                            }
                        }

                        ctx.Refresh();

                        if (respone.Result.All(r => r.Progress == 100)) { break; }
                        await Task.Delay(2000);
                        counter++;
                    }
                });
            }
        }
    }
}