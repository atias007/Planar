using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Job;

namespace SomeJob
{
    public class WorkerChild
    {
        private readonly IBaseJob _base;
        private readonly ILogger<WorkerChild> _logger;

        public WorkerChild(IServiceProvider serviceProvider)
        {
            _base = serviceProvider.GetRequiredService<IBaseJob>();
            _ = serviceProvider.GetRequiredService<GeneralUtil>();
            _logger = serviceProvider.GetRequiredService<ILogger<WorkerChild>>();
        }

        public void TestMe()
        {
            _logger.LogInformation("Test IBaseJob: {JobRunTime}", _base.JobRunTime);
        }
    }
}