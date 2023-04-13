using Quartz;
using Quartz.Util;

namespace Planar.Common.Helpers
{
    public static class KeyHelper
    {
        public static string GetKeyTitle<T>(Key<T> key)
        {
            var title = $"{key.Group}.{key.Name}";
            return title;
        }

        public static bool Equals<T>(Key<T> keyA, Key<T> keyB)
        {
            return keyA.Name == keyB.Name && keyA.Group == keyB.Group;
        }
    }
}