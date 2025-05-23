﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Job;

namespace EFCoreTest
{
    internal class JobInstanceLogTracker : BaseJob
    {
        public long LastId { get; set; }

        public override void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context)
        {
        }

        public override async Task ExecuteJob(IJobExecutionContext context)
        {
            Logger.LogInformation("Start with id {Id}", LastId);

            var dbContext = ServiceProvider.GetRequiredService<PlanarDbContext>();
            var count = await dbContext.JobInstanceLogs.Where(l => l.Id > LastId).CountAsync();
            LastId = await dbContext.JobInstanceLogs.OrderByDescending(l => l.Id).Select(l => l.Id).FirstOrDefaultAsync();
            Logger.LogInformation("Total items {Count}", count);
            Logger.LogInformation("Last id {Id}", LastId);

            var baseJob = ServiceProvider.GetRequiredService<IBaseJob>();
            await SetEffectedRowsAsync(1);
        }

        public override void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context)
        {
            var config = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
            var connectionString = config["Connection String"];
            services.AddDbContext<PlanarDbContext>(option => option.UseSqlServer(connectionString));
        }
    }
}