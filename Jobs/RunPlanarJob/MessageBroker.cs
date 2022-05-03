using Newtonsoft.Json;
using Quartz;
using RunPlanarJob.MessageBrokerEntities;

namespace RunPlanarJob
{
    internal class MessageBroker
    {
        private readonly IJobExecutionContext _context;

        public MessageBroker(IJobExecutionContext context)
        {
            _context = context;
        }

        public string Publish(string channel, string message)
        {
            switch (channel)
            {
                case "PutJobData":
                    var data1 = Serialize<KeyValueItem>(message);
                    _context.JobDetail.JobDataMap.Put(data1.Key, data1.Value);
                    return null;

                case "PutTriggerData":
                    var data2 = Serialize<KeyValueItem>(message);
                    _context.JobDetail.JobDataMap.Put(data2.Key, data2.Value);
                    return null;

                default:
                    return null;
            }
        }

        public static T Serialize<T>(string message)
        {
            var result = JsonConvert.DeserializeObject<T>(message);
            return result;
        }
    }
}