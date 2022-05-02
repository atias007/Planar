using Quartz;

namespace RunPlanarJob
{
    internal class MessageBroker
    {
        private readonly IJobExecutionContext _context;

        public MessageBroker(IJobExecutionContext context)
        {
            _context = context;
        }

        public void SendMessage(string message)
        {
        }
    }
}