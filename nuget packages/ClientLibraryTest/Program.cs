using Microsoft.Extensions.DependencyInjection;
using Planar.Client;
using Planar.Client.Entities;

var services = new ServiceCollection();
services.AddPlanarClient(c => c.Host = "http://localhost:2306");
var provider = services.BuildServiceProvider();
var client = provider.GetRequiredService<IPlanarClient>();

var odata = await client.History.ODataAsync(new ODataFilter { Filter = "triggerid eq 'manual'", Select = "xx,jobid" });

var details = await client.Job.GetAsync("Demo.HelloWorld");
Console.WriteLine(details.Active);
details = await client.Job.GetAsync("Infrastructure.BankOfIsraelCurrency");
Console.WriteLine(details.Active);
details = await client.Job.GetAsync("Monitoring.HealthCheck");
Console.WriteLine(details.Active);

var group = await client.Group.GetAsync("Admins");
var report = await client.Report.GetAsync(ReportNames.Summary);
//var result = await client.History.ListAsync(new ListHistoryFilter { HasWarnings = true });
//await client.Trigger.UpdateIntervalAsync("g2otp1mody4", TimeSpan.FromMinutes(55));
//await client.Trigger.UpdateCronExpressionAsync("jh4eums0jly", "0 0 18 ? 1/1 7#1 *"); // 0 0 16 ? 1/1 7#1 *

var trigger = await client.Trigger.GetAsync("g2otp1mody4");
Console.WriteLine(trigger.SimpleTriggers[0].Timeout);
await client.Trigger.UpdateTimeoutAsync("g2otp1mody4", TimeSpan.FromMinutes(115));
trigger = await client.Trigger.GetAsync("g2otp1mody4");
Console.WriteLine(trigger.SimpleTriggers[0].Timeout);

await client.Trigger.ClearTimeoutAsync("g2otp1mody4");
trigger = await client.Trigger.GetAsync("g2otp1mody4");
Console.WriteLine(trigger.SimpleTriggers[0].Timeout);

var list = await client.History.ListAsync(new ListHistoryFilter { PageSize = 5, PageNumber = 1, JobId = "Demo.TestEnvironmentExit" });

//await TestGroup(client);

//await TestUsers(client);

//await client.Jobs.TestAsync("Infrastructure.BankOfIsraelCurrency", DoIt);

//var metrics = await client.Metrics.ListMetricsAsync();

var summ = await client.Report.GetAsync(ReportNames.Summary);

var res0 = await client.Monitor.ListAsync();
var res1 = await client.Trigger.ListAsync("Infrastructure.BankOfIsraelCurrency");
var res2 = await client.History.GetAsync("NON_CLUSTERED638306436561775750");
var res3 = await client.History.LastAsync();

return;
// Console.WriteLine($"[x] Login as {login.Role}");
var result1 = await client.Job.GetJobTypesAsync();
Console.WriteLine($"[x] Job Types:");
foreach (var jobType in result1)
{
    Console.WriteLine($"    - {jobType}");
}

Console.WriteLine($"[x] List Job:");
var result2 = await client.Job.ListAsync();

foreach (var job in result2.Data!)
{
    Console.WriteLine($"    - {job.Id} - {job.Group}.{job.Name}");
}

Console.WriteLine($"[x] Get Job:");
var id = result2.Data!.First().Id;
var result3 = await client.Job.GetAsync(id);
Console.WriteLine($"    - {result3.Description}");

Console.WriteLine($"[x] Get Job File:");
var result4 = await client.Job.GetJobFileAsync(result1.First());
var result5 = await client.Job.DescribeJobAsync("Infrastructure.BankOfIsraelCurrency");
await client.Job.ResumeAsync("5c1sgknnaj5");
Thread.Sleep(2000);
var result6 = await client.Job.GetNextRunningAsync("Infrastructure.BankOfIsraelCurrency");
var result7 = await client.Job.GetPreviousRunningAsync("5c1sgknnaj5");
await client.Job.PauseAsync("5c1sgknnaj5");

Console.ReadLine();

static async Task DoIt(RunningJobDetails data)
{
    await Console.Out.WriteLineAsync(data.Progress.ToString());
}

static async Task TestGroup(PlanarClient client)
{
    await client.Group.AddAsync(new Group { Name = "ClientTest", AdditionalField1 = "Test1", Role = Roles.Editor });
    var group = await client.Group.GetAsync("ClientTest");
    await Console.Out.WriteLineAsync(group.AdditionalField1);
    var all = await client.Group.ListAsync();
}

static async Task TestUsers(PlanarClient client)
{
    var users = await client.User.ListAsync();
}