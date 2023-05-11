namespace SomeJob
{
    public class WorkerChild
    {
        private readonly IServiceProvider serviceProvider;

        public WorkerChild(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }
    }
}