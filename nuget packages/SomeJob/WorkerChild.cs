using Microsoft.Extensions.DependencyInjection;
using Planar.Job;

namespace SomeJob
{
    public class WorkerChild
    {
        public WorkerChild(IServiceProvider serviceProvider)
        {
            var basej = serviceProvider.GetRequiredService<IBaseJob>();
            var util = serviceProvider.GetRequiredService<GeneralUtil>();
        }
    }
}