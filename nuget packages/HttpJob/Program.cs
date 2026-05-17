using HttpJob;
using Planar.Job;
using Planar.Job.Http;

var properties = new HttpJobStartPropertiesBuilder()
        .WithPlanarHostName("localhost")
        .AddJob<SomeJob>("somejob")
        .Build();

await PlanarJob.StartAsync(properties);