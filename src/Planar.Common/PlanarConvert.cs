﻿namespace Planar.Common
{
    public static class PlanarConvert
    {
        public static string? ToString(object? value)
        {
            var strValue = value == null ? null : System.Convert.ToString(value);
            return strValue;
        }
    }
}