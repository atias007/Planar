using Quartz;
using Quartz.Util;

namespace Planar.Common.Helpers
{
    public static class KeyHelper
    {
        public static string GetKeyTitle<T>(Key<T> key)
        {
            if (key.Group == Key<T>.DefaultGroup)
            {
                return key.Name;
            }

            var title = $"{key.Group}.{key.Name}";
            return title;
        }

        public static bool Equals<T>(Key<T> keyA, Key<T> keyB)
        {
            return keyA.Name == keyB.Name && keyA.Group == keyB.Group;
        }
    }
}