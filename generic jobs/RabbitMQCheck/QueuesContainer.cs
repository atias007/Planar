using Common;

namespace RabbitMQCheck;

internal class QueuesContainer(IEnumerable<Queue> queues) : BaseDefault, ICheckElemnt
{
    public IEnumerable<Queue> Queues { get; set; } = queues;
    public string Key => "queues";
}