using System.Collections.Concurrent;
using System.Threading;

namespace CommonJob;

internal class LimitQueue<T>(int limit) : ConcurrentQueue<T>
{
    private readonly int _limit = limit;
    private readonly Lock _locker = new();

    public new void Enqueue(T item)
    {
        base.Enqueue(item);
        lock (_locker)
        {
            while (Count > _limit && TryDequeue(out _))
            {
                // === DO NOTHING ===
            }
        }
    }
}