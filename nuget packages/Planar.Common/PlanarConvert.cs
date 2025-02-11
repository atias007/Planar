namespace Planar.Common
{
    internal static class PlanarConvert
    {
#if NETSTANDARD2_0

        public static string ToString(object value)
#else
        public static string? ToString(object? value)
#endif
        {
            var strValue = value == null ? null : System.Convert.ToString(value);
            return strValue;
        }
    }
}