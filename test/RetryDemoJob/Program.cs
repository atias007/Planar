using Planar.Job;
using RetryDemoJob;

await PlanarJob.StartAsync<Worker>();