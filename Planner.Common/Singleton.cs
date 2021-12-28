using System;

namespace Planner.Common
{
    public class Singleton<T>
        where T : class
    {
        private readonly Func<T> _instanceCreator;
        private T _instance = null;
        private static readonly object Locker = new();

        public Singleton(Func<T> instanceCreator = null)
        {
            _instanceCreator = instanceCreator;
        }

        public T Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (Locker)
                    {
                        if (_instance == null)
                        {
                            if (_instanceCreator == null)
                            {
                                _instance = Activator.CreateInstance<T>();
                            }
                            else
                            {
                                _instance = _instanceCreator.Invoke();
                            }
                        }
                    }
                }

                return _instance;
            }
        }
    }
}