using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.General
{
    internal static class LockUtil
    {
        private static readonly ConcurrentDictionary<string, byte> _bag = new();
        private static readonly object _locker = new();

        public static bool TryLock(string key)
        {
            lock (_locker)
            {
                if (_bag.ContainsKey(key)) { return false; }

                return _bag.TryAdd(key, 0);
            }
        }

        public static bool TryLock(string key, TimeSpan span)
        {
            lock (_locker)
            {
                if (_bag.ContainsKey(key)) { return false; }

                _ = Task.Run(() =>
                {
                    Thread.Sleep(span);
                    Release(key);
                });

                return _bag.TryAdd(key, 0);
            }
        }

        public static bool Release(string key)
        {
            return _bag.TryRemove(key, out byte _);
        }
    }
}