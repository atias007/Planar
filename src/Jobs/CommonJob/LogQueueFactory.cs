using Planar;
using System.Collections.Generic;

namespace CommonJob;

public class LogQueueFactory
{
    private LogQueueFactory()
    {
    }

    private static readonly LogQueueFactory _logQueueFactory = new();

    public static LogQueueFactory Instance => _logQueueFactory;

    private const int limit = 50;
    private readonly object Locker = new();
    private readonly Dictionary<string, LimitQueue<LogEntity>> Queues = [];

    private LimitQueue<LogEntity> GetQueue(string key)
    {
        lock (Locker)
        {
            if (Queues.TryGetValue(key, out var queue))
            {
                return queue;
            }

            queue = new LimitQueue<LogEntity>(limit);
            Queues.Add(key, queue);
            return queue;
        }
    }

    public void Enqueue(string fireInstanceId, LogEntity log)
    {
        var queue = GetQueue(fireInstanceId);
        queue.Enqueue(log);
    }

    public LogEntity? Dequeue(string fireInstanceId)
    {
        var queue = GetQueue(fireInstanceId);
        if (queue.TryDequeue(out var result)) { return result; }
        return null;
    }

    public void Clear(string fireInstanceId)
    {
        Queues.Remove(fireInstanceId);
    }
}