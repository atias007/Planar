using Planar.Client;
using Planar.Client.Entities;

var client = new PlanarClient();
var login = await client.ConnectAsync("http://localhost:2306");

//await client.Jobs.TestAsync("Infrastructure.BankOfIsraelCurrency", DoIt);

var res1 = await client.History.Get(24610);
var res2 = await client.History.Get("NON_CLUSTERED638306436561775750");

return;
Console.WriteLine($"[x] Login as {login.Role}");
var result1 = await client.Jobs.GetJobTypesAsync();
Console.WriteLine($"[x] Job Types:");
foreach (var jobType in result1)
{
    Console.WriteLine($"    - {jobType}");
}

Console.WriteLine($"[x] List Job:");
var result2 = await client.Jobs.ListAsync();

foreach (var job in result2.Data!)
{
    Console.WriteLine($"    - {job.Id} - {job.Group}.{job.Name}");
}

Console.WriteLine($"[x] Get Job:");
var id = result2.Data![0].Id;
var result3 = await client.Jobs.GetAsync(id);
Console.WriteLine($"    - {result3.Description}");

Console.WriteLine($"[x] Get Job File:");
var result4 = await client.Jobs.GetJobFileAsync(result1.First());
var result5 = await client.Jobs.DescribeJobAsync("Infrastructure.BankOfIsraelCurrency");
await client.Jobs.ResumeAsync("5c1sgknnaj5");
Thread.Sleep(2000);
var result6 = await client.Jobs.GetNextRunningAsync("Infrastructure.BankOfIsraelCurrency");
var result7 = await client.Jobs.GetPreviousRunningAsync("5c1sgknnaj5");
await client.Jobs.PauseAsync("5c1sgknnaj5");

Console.ReadLine();

static async Task DoIt(RunningJobDetails data)
{
    await Console.Out.WriteLineAsync(data.Progress.ToString());
}