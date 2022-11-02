using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar;

namespace EFCoreTest
{
    internal class JobInstanceLogTracker : BaseJob
    {
        public int LastId { get; set; }

        public override void Configure(IConfigurationBuilder configurationBuilder, string environment)
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
        }

        public override void RegisterServices(IServiceCollection services)
        {
            var config = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
            var connectionString = config["Connection String"];
            services.AddDbContext<PlanarDbContext>(option => option.UseSqlServer(connectionString));
        }
    }
}