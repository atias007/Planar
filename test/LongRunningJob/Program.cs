using LongRunningJob;
using Planar.Job;

await PlanarJob.StartAsync<Worker>();