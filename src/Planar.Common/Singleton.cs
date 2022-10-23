using System;

namespace Planar.Common
{
    public class Singleton<T>
        where T : class
    {
        private readonly Func<T> _instanceCreator;
        private T _instance;
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
                        if (_instance == null) //-V3054
                        {
                            if (_instanceCreator == null) //-V3054
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