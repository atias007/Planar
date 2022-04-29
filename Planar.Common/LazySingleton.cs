using System;

namespace Planar.Common
{
    public class LazySingleton<T>
        where T : class
    {
        private static T _instance;
        private static readonly object Locker = new();
        private readonly Func<T> _instanceCreator;

        public LazySingleton(Func<T> instanceCreator = null)
        {
            _instanceCreator = instanceCreator ?? Activator.CreateInstance<T>;
        }

        public T Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (Locker)
                    {
                        if (_instance == null) //-V3054
                        {
                            _instance = _instanceCreator.Invoke();
                        }
                    }
                }

                return _instance;
            }
        }

        public void Flush()
        {
            if (_instance != null)
            {
                lock (Locker)
                {
                    if (_instance != null) //-V3054
                    {
                        _instance = null;
                    }
                }
            }
        }

        public void Reload()
        {
            Flush();
            _ = Instance;
        }
    }
}