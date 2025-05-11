using Planar.Job;
using MemoryLeak;

System.AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
{
    Environment.Exit(-1);
}

await PlanarJob.StartAsync<Job>();