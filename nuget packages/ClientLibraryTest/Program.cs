using Planar.Client;
using Planar.Client.Entities;

var client = new PlanarClient();
var login = await client.ConnectAsync("http://localhost:2306");

//await client.Jobs.TestAsync("Infrastructure.BankOfIsraelCurrency", DoIt);

var res0 = await client.Monitor.ListAsync();
var res1 = await client.Trigger.ListAsync("Infrastructure.BankOfIsraelCurrency");
var res2 = await client.History.GetAsync("NON_CLUSTERED638306436561775750");
var res3 = await client.History.LastAsync();

return;
Console.WriteLine($"[x] Login as {login.Role}");
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
var id = result2.Data![0].Id;
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