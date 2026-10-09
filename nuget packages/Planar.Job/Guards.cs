using System;

namespace Planar.Job
{
    internal static class Guards
    {
        public static void ThrowIfGreaterThan<T>(T value, T other, string paramName)
            where T : IComparable<T>
        {
            if (value.CompareTo(other) > 0)
            {
                throw new ArgumentOutOfRangeException(
                    paramName,
                    value,
                    $"{paramName} ('{value}') must be less than or equal to '{other}'.");
            }
        }
    }
}